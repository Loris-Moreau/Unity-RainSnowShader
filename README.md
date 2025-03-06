# Rain & Snow Geometry Shader : 

- rain/snow only generated around the camera with a grid system for performance

- the grid system also makes it look like the precipitations are coming from a static location (sky) rather than from the camera itself

- A mesh is created to serve as the base for rendering the precipitation. The mesh consists of multiple quads that represent each raindrop or snowflake.

- they're instanced & their base positions & rotations are randomized
  - (could use XORShift if it's more optimmized than the base random)

- culling (not render) everything behind the camera


- Precipitation needs to appear to fall. This is done by animating the 'Y' position of each vertex (particle) in the shader, giving the illusion of falling raindrops or snowflakes.

- The system also accounts for wind by rotating particles in a specified direction and strength, adding realism by making the precipitation move horizontally as well as vertically.

- opacity varies based on distance with camera, amount of precipitation & whether it's rain or snow

- snow always face the camera(billboarding) (they're small & will be unseen if not facing the cam)

- snow is uniform(circle-ish) while rain drops are elongated(rectangles) & the "wind" causes a "flutter" effect

---

# Showcase 

![Rain and Snow Shader Showcase](https://github.com/Loris-Moreau/Unity-RainSnowShader/blob/main/Showcase/Rainy_Snowy_Weather_Unity_2023.2.20f1.gif)
