# mivi

Musical Instrument Video Interface

Visualization for keyboard via midi

## Dependencies

### macOS

#### GLFW

glfw: `brew install glfw`

then, in `glfw-net/GLFW.NET/Glfw.cs`, update `LIBRARY`. (Change minor version of glfw changes).

```
public const string LIBRARY = "/usr/local/lib/libglfw.3.3.dylib";
```

Be sure to set hints before opening a window
