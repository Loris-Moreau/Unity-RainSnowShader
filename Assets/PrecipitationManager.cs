﻿using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

[ExecuteInEditMode] public class PrecipitationManager : MonoBehaviour 
{
    [System.Serializable] public class EnvironmentParticlesSettings
    {
        [Range(0, 1)] public float amount = 1.0f;
        public Color color = Color.white;

        [Tooltip("Alpha = variation amount")]
        public Color colorVariation = Color.white;
        public float fallSpeed;
        public Vector2 cameraRange; 
        public Vector2 flutterFrequency, flutterSpeed, flutterMagnitude;
        public Vector2 sizeRange;
        
        public EnvironmentParticlesSettings (
            Color color, Color colorVariation, 
            float fallSpeed, Vector2 cameraRange, 
            Vector2 flutterFrequency, Vector2 flutterSpeed, Vector2 flutterMagnitude, 
            Vector2 sizeRange
        ) {
            
            this.color = color;
            this.colorVariation = colorVariation;
            this.fallSpeed = fallSpeed;
            this.cameraRange = cameraRange;
            this.flutterFrequency = flutterFrequency;
            this.flutterSpeed = flutterSpeed;
            this.flutterMagnitude = flutterMagnitude;
            this.sizeRange = sizeRange;
        }
    }

    public Texture2D mainTexture;
    public Texture2D noiseTexture;

    [Range(0,1)] public float windStrength;
    [Range(-180,180)] public float windYRotation;
		
    // 65536 (256 x 256) vertices is the max per mesh
    [Range(2, 256)] public int meshSubdivisions = 200;

    // populate the settings with some initial values
    public EnvironmentParticlesSettings rain = new EnvironmentParticlesSettings(
        Color.white, Color.white, 3,  // color, colorVariation, fall speed
        new Vector2(0,15), //camera range
        new Vector2(0.988f, 1.234f), //flutter frequency
        new Vector2(.01f, .01f), //flutter speed
        new Vector2(.35f, .25f), //flutter magnitude
        new Vector2(.5f, 1f)//, //size range 
    );
    
    public EnvironmentParticlesSettings snow = new EnvironmentParticlesSettings(	
        Color.white, Color.white, .25f,  // color, colorVariation, fall speed
        new Vector2(0,10), //camera range
        new Vector2(0.988f, 1.234f), //flutter frequency
        new Vector2(1f, .5f), //flutter speed
        new Vector2(.35f, .25f), //flutter magnitude
        new Vector2(.05f, .025f)//, //size range 
    );

    GridHandler gridHandler;
    Matrix4x4[] renderMatrices = new Matrix4x4[3 * 3 * 3];
		
    Mesh meshToDraw;

    Material rainMaterial, snowMaterial;
    // automatic material creation
    static Material CreateMaterialIfNull(string shaderName, ref Material reference) {
        if (reference == null) {
            reference = new Material(Shader.Find(shaderName));
            reference.hideFlags = HideFlags.HideAndDontSave;
            reference.renderQueue = 3000;
            reference.enableInstancing = true;
        }
        return reference;
    }

    void OnEnable () {
        gridHandler = GetComponent<GridHandler>();
        gridHandler.onPlayerGridChange += OnPlayerGridChange;
    }

    void OnDisable() {
        gridHandler.onPlayerGridChange -= OnPlayerGridChange;
    }
    
    /*
        set all our render matrices to be positioned
        in a 3x3x3 grid around the player
    */
    void OnPlayerGridChange(Vector3Int playerGrid) {

        // index for each individual matrix
        int i = 0;

        // loop in a 3 x 3 x 3 grid
        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                for (int z = -1; z <= 1; z++) {

                    Vector3Int neighborOffset = new Vector3Int(x, y, z);
                    
                    // adjust the rendering position matrix, leaving rotation and scale alone
                    renderMatrices[i++].SetTRS(
                        gridHandler.GetGridCenter(playerGrid + neighborOffset), 
                        Quaternion.identity, 
                        Vector3.one
                    );
                }
            }
        }
    }

    void Update()
    {
        // update the mesh automatically if it doesnt exist
        if (meshToDraw == null)
            RebuildPrecipitationMesh();

        // the higher the windstrength, the more the precipitation
        // "leans" in the direction of the wind (with a max lean angle of 45 degrees)
        float windStrengthAngle = Mathf.Lerp(0, 45, windStrength);

        Vector3 windRotationEulerAngles = new Vector3(
            -windStrengthAngle,
            windYRotation,
            0
        );

        // we need to supply the shader with the rotation matrix so it can "fall" in the correct direction
        Matrix4x4 windRotationMatrix = Matrix4x4.TRS(
            Vector3.zero, Quaternion.Euler(windRotationEulerAngles), Vector3.one
        );
			
        /*
            when falling straight down, the max travel distance of a particle is
            the grid size.  but when we account for the wind angle, we have to consider
            the max travel distance the hypotenuse of a right triangle with it's adjacent
            side as the grid size

                    |\
                    |a\         
	  gridSize  |  \
                    |   \
                    |    \

            cos(a) = gridSize / maxTravelDistance 
            maxTravelDistance = gridSize / cos(a)
        */
        float maxTravelDistance = gridHandler.gridSize / Mathf.Cos(windStrengthAngle * Mathf.Deg2Rad);

        // render the rain and snow
        RenderEnvironmentParticles(rain, CreateMaterialIfNull("Hidden/Environment/Rain", ref rainMaterial), maxTravelDistance, windRotationMatrix);
        RenderEnvironmentParticles(snow, CreateMaterialIfNull("Hidden/Environment/Snow", ref snowMaterial), maxTravelDistance, windRotationMatrix);
    }

    void RenderEnvironmentParticles(EnvironmentParticlesSettings settings, Material material, float maxTravelDistance, Matrix4x4 windRotationMatrix) {

        // if the amount is 0, dont render anything
        if (settings.amount <= 0)
            return;

        material.SetTexture("_MainTex", mainTexture);
        material.SetTexture("_NoiseTex", noiseTexture);  

        material.SetFloat("_GridSize", gridHandler.gridSize);
        
        material.SetFloat("_Amount", settings.amount);

        material.SetColor("_Color", settings.color);
        material.SetColor("_ColorVariation", settings.colorVariation);
        material.SetFloat("_FallSpeed", settings.fallSpeed);
        material.SetVector("_FlutterFrequency", settings.flutterFrequency);
        material.SetVector("_FlutterSpeed", settings.flutterSpeed);
        material.SetVector("_FlutterMagnitude", settings.flutterMagnitude);
        material.SetVector("_CameraRange", settings.cameraRange);
        material.SetVector("_SizeRange", settings.sizeRange);

        material.SetFloat("_MaxTravelDistance", maxTravelDistance);

        material.SetMatrix("_WindRotationMatrix", windRotationMatrix);
                        
        Graphics.DrawMeshInstanced(
            meshToDraw, 0, material, renderMatrices, renderMatrices.Length, 
            null, ShadowCastingMode.Off, true, 0, null, LightProbeUsage.Off
        );
    }

    // the mesh created has a 
    // center at [0,0], 
    // min at [-0.5, -0.5] 
    // max at [0.5, 0.5]
    public void RebuildPrecipitationMesh() {
        Mesh mesh = new Mesh ();
        List<int> indicies = new List<int>();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> uvs = new List<Vector3>();
        
        // use 0 - 100 range instead of 0 to 1 to avoid precision errors when subdivisions are to high
        float f = 100f / meshSubdivisions;
        int i  = 0;
        for (float x = 0.0f; x <= 100f; x += f) {
            for (float y = 0.0f; y <= 100f; y += f) {

                // normalize x and y to a value between 0 and 1
                float x01 = x / 100.0f;
                float y01 = y / 100.0f;

                vertices.Add(new Vector3(x01 - .5f, 0, y01 - .5f));

                // calcualte the threshold for this vertex to recreate the 'thinning out' effect
                float vertexIntensityThreshold = Mathf.Max(
                    (float)((x / f) % 4.0f) / 4.0f, 
                    (float)((y / f) % 4.0f) / 4.0f
                );

                // store the `vertexIntensityThreshold` value as the z component in the uv's
                uvs.Add(new Vector3(x01, y01, vertexIntensityThreshold));

                indicies.Add(i++);
            }    
        }
        
        mesh.SetVertices(vertices);
        mesh.SetUVs(0,uvs);
        mesh.SetIndices(indicies.ToArray(), MeshTopology.Points, 0);

        // give a large bounds so it's always visible, we'll handle culling manually
        mesh.bounds = new Bounds(Vector3.zero, new Vector3(500, 500, 500));

        // dont save as an asset
        mesh.hideFlags = HideFlags.HideAndDontSave;

        meshToDraw = mesh;
    } 
}

#if UNITY_EDITOR
// create a custom editor with a button to trigger rebuilding of the render mesh
[CustomEditor(typeof(PrecipitationManager))] 
public class PrecipitationManagerEditor : Editor {

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        
        if (GUILayout.Button("Rebuild Precipitation Mesh")) {
            (target as PrecipitationManager).RebuildPrecipitationMesh();

            // set dirty to make sure the editor updates
            EditorUtility.SetDirty(target);
        }
    }
}
#endif
