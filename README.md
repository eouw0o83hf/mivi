# mivi

Musical Instrument Video Interface

Visualization for keyboard via midi

## Libraries / Technologies

### MIDI

- managed-midi: dotnet core wrapper for MIDI

### Graphics

#### In use

- OpenGL: Low-level GPU API
- GLFW: Multi-platform OpenGl wrapper
- glfw-net: dotnet core wrapper for GLFW and a couple of other libs

#### Possible future use

- GLM (OpenGl Mathematics): CPU-side vector math library
- GlmNet: dotnet core wrapper for GLM

## Dependencies

### macOS

#### GLFW

glfw: `brew install glfw`

then, in `glfw-net/GLFW.NET/Glfw.cs`, update `LIBRARY`. (Change minor version of glfw changes).

```
public const string LIBRARY = "/usr/local/lib/libglfw.3.3.dylib";
```

Be sure to set hints before opening a window

## Reference Materials

Personal tracking for future reference:

- [managed-midi](https://github.com/atsushieno/managed-midi) MIDI adapter
- [glfw-net](https://github.com/ForeverZer0/glfw-net) GLFW netcore adapter
- [Model View Projection matrix math](http://www.opengl-tutorial.org/beginners-tutorials/tutorial-3-matrices/)
- [Learn OpenGL API reference](https://learnopengl.com/Getting-started/Hello-Triangle)
- [GLSL Data Types](<https://www.khronos.org/opengl/wiki/Data_Type_(GLSL)#Vectors>) for shader source
- [GlmNet](https://github.com/dwmkerr/glmnet) for when we need advanced matrix math run before shaders take over
