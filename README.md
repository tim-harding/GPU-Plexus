# GPU Plexus
A large-scale plexus effect that is updated entirely on the GPU in Unity.  Used to test compute shaders for visual effects applications.  The example scene uses a 32x32x32 grid, running at ~3000fps on a 1080.

* Builds a grid of points on the CPU on frame 1
* Updates each point with perlin noise and convergence around a point in space per frame in a compute shader
* Connections to neighboring grid cells are generated in the geometry shader and rendered as lines, using positions provided by the compute shader in a structured buffer
* Shaded based on distance to neightboring points

![](https://imgur.com/08Ky1SE.png)
