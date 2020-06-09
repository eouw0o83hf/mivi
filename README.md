# mivi

Musical Instrument Video Interface

A cross-platform MIDI visualizer. Written in C# on dotnet core, using OpenGL for rendering.

## Executing

`cd` to `src/Mivi.Console` and enter `dotnet run` at the command line. Note that you must have [dotnet core sdk 3.1+ installed](https://dotnet.microsoft.com/download/dotnet-core/3.1).

Example visualization

![fur elise](https://user-images.githubusercontent.com/2204030/83962879-ecba8a00-a866-11ea-97cf-dbe77f93d150.gif)

## Architecture

This section is a plan, not necessarily reflective of current reality.

There are two architectural divisions to the application:

- Event processing side: series of event processing pipelines which result in a simple `State` object
- Graphics side: GLFW/OpenGL-driven graphics rendering driven by `State`

The two are married up by the composition root which links them with the shared `State` object.

### Event processing side

Use a centralized `EventBus` which takes in strongly-typed versions of MIDI events. Inputs to the `EventBus` will be:

- `MIDI Bus Adapter` discussed below
- `FIDI` fake MIDI input driven from computer keyboard if no piano is available
- `Timer` to provide regular tick events

`MIDI Bus Adapter` will be the sole listener to the MIDI bus provided by managed-midi. It adapts raw MIDI events into strongly-typed versions for easier consumption.

`Timer` ticks quickly and regularly, which allows effects to process deterministically in consumers for ease of testing and API

The `EventBus` will have any number of `Consumers` which monitor it for activity relevant to their mandates. Examples of `Consumers` would be:

- Determine output volume per note index given depressed keys and attenuation over time
- Detect staccato notes so that cool effects can happen
- Detect certain chord structures so that cool effects can happen

State will be shared between `Consumers` and the graphics side via a shared `State` object. `State` should be populated largely with primitive arrays which are each owned by a single `Consumer`. (It may make sense to make `State` a partial file and let each `Consumer` dictate its output in the same file as its definition.)

### Graphics side

`State` is the go-between for event processing and graphics, and the display should essentially be a projection of `State`. OpenGL provides a core loop, so the graphics side will poll `State`, while `State` is updated asynchronously on the other side as a result of the event pipelines.

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

### dotnet core 3.1

You must have the dotnet core 3.1 SDK installed. Downloads available here:

https://dotnet.microsoft.com/download/dotnet-core/3.1

### GLFW

This application depends on glfw, which can be installed with any package manager. Unfortunately it installs with wildly different names and locations per OS, so I have inlined the GLFW binaries with this codebase.

## Reference Materials

Personal tracking for future reference:

- [managed-midi](https://github.com/atsushieno/managed-midi) MIDI adapter
- [glfw-net](https://github.com/ForeverZer0/glfw-net) GLFW netcore adapter
- [Model View Projection matrix math](http://www.opengl-tutorial.org/beginners-tutorials/tutorial-3-matrices/)
- [Learn OpenGL API reference](https://learnopengl.com/Getting-started/Hello-Triangle)
- [GLSL Data Types](<https://www.khronos.org/opengl/wiki/Data_Type_(GLSL)#Vectors>) for shader source
- [GlmNet](https://github.com/dwmkerr/glmnet) for when we need advanced matrix math run before shaders take over
