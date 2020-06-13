using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using GLFW;
using GlmNet;
using Mivi.Core;
using static OpenGL.Gl;
using Exception = System.Exception;
using SConsole = System.Console;

namespace Mivi.Console
{
    public class GraphicsApplication
    {
        private readonly KeyboardEvents _keyboardEvents;
        private readonly SharedState _state;

        public GraphicsApplication(KeyboardEvents keyboardEvents, SharedState state)
        {
            _keyboardEvents = keyboardEvents;
            _state = state;
        }

        private const float ticksPerZUnit = 50f;

        public unsafe void Launch()
        {
            // Set context creation hints
            PrepareContext();

            // Create a window and shader program
            var window = CreateWindow();
            var program = CreateProgram();

            // Enable anti-aliasing
            glEnable(GL_MULTISAMPLE);
            // enable depth testing to avoid
            // z-indexing issues
            glEnable(GL_DEPTH_TEST);
            glDepthFunc(GL_LEQUAL);

            Glfw.SetKeyCallback(window, KeyCallback);

            // Create present-key containers
            var vertexContainers = CreateVertices(88);
            // Past-key containers
            var pastVertexContainers = CreateVertices(_state.PastNotes.Length);

            var colorLocation = glGetUniformLocation(program, "color");
            var tickLocation = glGetUniformLocation(program, "ticks");

            // Projection matrix : 45° Field of View, 4:3 ratio, display range : 0.1 unit <-> 100 units
            var projectionMatrix = glm.perspective(
                glm.radians(45f),
                1f, 0.1f, 100f
            );

            var viewMatrix = glm.lookAt(
               new vec3(0f, 2.0f, 1.3f),    // eye
               new vec3(0, -0.35f, -1.0f),       // center
               new vec3(0, 1, 0)            // up
           );

            var mvpMatrixLocation = glGetUniformLocation(program, "mvp");

            var colorProvider = new KeyColorProvider();
            // float so that we never have to worry about overflow
            // and we can make it a little more granular
            var ticksStep = 0.1f;
            var ticksFloat = 0f;

            while (!Glfw.WindowShouldClose(window))
            {
                // TODO wire this up to the main bus clock
                // instead of the ui loop
                colorProvider.Tick();
                ticksFloat += ticksStep;
                glUniform1f(tickLocation, ticksFloat);

                // Swap fore/back framebuffers, and poll for operating system events.
                Glfw.SwapBuffers(window);
                Glfw.PollEvents();

                // Clear the framebuffer to defined background color
                glClear(GL_COLOR_BUFFER_BIT);
                // Clear depth buffer to avoid flickering
                glClear(GL_DEPTH_BUFFER_BIT);

                glUseProgram(program);

                var keys = _state.NoteVelocities;

                // Draw current notes
                foreach (var (x, i) in vertexContainers.WithIndex())
                {
                    var adjustedIndex = i + MidiNote.LowestPianoIndex;

                    var velocity = keys[adjustedIndex];
                    if (velocity <= 0.001f)
                    {
                        continue;
                    }

                    var color = colorProvider.GetColor(adjustedIndex);

                    glUniform3f(colorLocation, color[0], color[1], color[2]);

                    glBindVertexArray(x.VertexArray);

                    // top two of the rightmost column
                    var translateMatrix = new mat4(1.0f);
                    translateMatrix[3, 0] = KeyUnitWidth * i - 1f; // x position
                    translateMatrix[3, 1] = -1; // y position

                    var rotationMatrix = new mat4(1.0f);

                    var scaleMatrix = new mat4(1.0f);
                    scaleMatrix[0, 0] = 1f; // x scale
                    scaleMatrix[1, 1] = scaleVolume(velocity); // y scale
                    scaleMatrix[2, 2] = _state.NoteLengths[adjustedIndex] / ticksPerZUnit;

                    var modelMatrix = translateMatrix * rotationMatrix * scaleMatrix;

                    var mvpMatrix = projectionMatrix * viewMatrix * modelMatrix;

                    glUniformMatrix4fv(mvpMatrixLocation, 1, false, mvpMatrix.to_array());

                    glDrawElements(GL_TRIANGLES, 36, GL_UNSIGNED_INT, NULL);
                }

                // Draw past notes
                for (var i = 0; i < _state.PastNotes.Length; ++i)
                {
                    var pastNote = _state.PastNotes[i];
                    if (pastNote == null)
                    {
                        continue;
                    }

                    var adjustedIndex = pastNote.Index - MidiNote.LowestPianoIndex;

                    var color = colorProvider.GetColor(pastNote.Index);
                    glUniform3f(colorLocation, color[0], color[1], color[2]);

                    glBindVertexArray(pastVertexContainers[i].VertexArray);

                    // top two of the rightmost column
                    var translateMatrix = new mat4(1.0f);
                    translateMatrix[3, 0] = KeyUnitWidth * adjustedIndex - 1f; // x position
                    translateMatrix[3, 1] = -1; // y position
                    translateMatrix[3, 2] = -pastNote.TicksSinceKeyUp / ticksPerZUnit; // z position

                    var rotationMatrix = new mat4(1.0f);

                    var scaleMatrix = new mat4(1.0f);
                    scaleMatrix[0, 0] = 1f; // x scale
                    scaleMatrix[1, 1] = scaleVolume(pastNote.Velocity); // y scale
                    scaleMatrix[2, 2] = pastNote.Length / ticksPerZUnit;

                    var modelMatrix = translateMatrix * rotationMatrix * scaleMatrix;

                    var mvpMatrix = projectionMatrix * viewMatrix * modelMatrix;

                    glUniformMatrix4fv(mvpMatrixLocation, 1, false, mvpMatrix.to_array());

                    glDrawElements(GL_TRIANGLES, 36, GL_UNSIGNED_INT, NULL);
                }
            }

            Glfw.Terminate();
        }

        // Unfortunately dotnet core does not provide cross-platform global
        // (or even in-application) keyboard hooks. Fortunately, GLFW happens
        // to do that. So, we need to feed data back to the main part of the
        // app from this component since it needs to pull double duty.
        private void KeyCallback(Window window, Keys key, int scanCode, InputState state, ModifierKeys mods)
            => _keyboardEvents.PushKeyChange((int)key, state switch
            {
                InputState.Press => KeyboardEventTypes.Pressed,
                InputState.Release => KeyboardEventTypes.Released,
                InputState.Repeat => KeyboardEventTypes.Repeated,
                _ => 0
            });

        // This should probably be logarithmic
        private static float scaleVolume(float midiVelocity) => midiVelocity / 300f;

        private static readonly Random _random = new Random();

        private static float[] GetRandomColor()
        {
            var r = (float)_random.NextDouble();
            var g = (float)_random.NextDouble();
            var b = (float)_random.NextDouble();
            return new[] { r, g, b };
        }

        private static void PrepareContext()
        {
            // Set some common hints for the OpenGL profile creation
            try
            {
                Glfw.WindowHint(Hint.ClientApi, ClientApi.OpenGL);
            }
            catch (TypeInitializationException ex)
                when (ex.InnerException is DllNotFoundException d)
            {
                SConsole.WriteLine("Could not find GLFW DLL");
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    SConsole.WriteLine("You need to add the glfw3 library's directory to the PATH");
                }
                else
                {
                    SConsole.WriteLine("You need to add the glfw library's directory to the PATH. Recommend running:");
                    SConsole.WriteLine("export PATH=$PATH:/usr/local/include");
                }
                Environment.Exit(1);
            }
            Glfw.WindowHint(Hint.ContextVersionMajor, 3);
            Glfw.WindowHint(Hint.ContextVersionMinor, 3);
            Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);
            Glfw.WindowHint(Hint.Doublebuffer, true);
            Glfw.WindowHint(Hint.Decorated, true);
            Glfw.WindowHint(Hint.OpenglForwardCompatible, true);

            Glfw.WindowHint(Hint.Samples, 8);
        }

        /// <summary>
        /// Creates and returns a handle to a GLFW window with a current OpenGL context.
        /// </summary>
        /// <param name="width">The width of the client area, in pixels.</param>
        /// <param name="height">The height of the client area, in pixels.</param>
        /// <returns>A handle to the created window.</returns>
        private static Window CreateWindow()
        {
            var screen = Glfw.PrimaryMonitor.WorkArea;

            // Create window, make the OpenGL context current on the thread, and import graphics functions
            var window = Glfw.CreateWindow(screen.Width, screen.Height, "eouw0o83hf MIVI", Monitor.None, Window.None);
            Glfw.MakeContextCurrent(window);
            Import(Glfw.GetProcAddress);

            return window;
        }

        private const string TriangleVertexShader = @"
#version 330 core

layout (location = 0) in vec3 pos;

out vec4 position;

uniform mat4 mvp;

void main()
{
    position = mvp * vec4(pos, 1.0);
    gl_Position = position;
}
";

        private const string TriangleFragmentShader = @"
#version 330 core
in vec4 position;

out vec4 result;

uniform vec3 color;
uniform float ticks;

float rectify(float x) {
    return (x + 1.0) / 2.0;
}

void main()
{
    float adjustedTicks = ticks / 17.0;
    vec3 adjustedCoord = vec3(position / 40.0);

    float colorSeed =
        (adjustedCoord.x * 4.0)
        + (adjustedCoord.y / 2.5)
        + adjustedTicks;

    float r = rectify(cos(colorSeed));
    float g = rectify(cos(colorSeed + 2.1));
    float b = rectify(cos(colorSeed + 4.2));
    float a = 1.0;
    result = vec4(r, g, b, a);
    result = result + 0.1;
}
";

        private static uint CreateProgram()
        {
            var vertex = CreateShader(GL_VERTEX_SHADER, TriangleVertexShader);
            var fragment = CreateShader(GL_FRAGMENT_SHADER, TriangleFragmentShader);

            var program = glCreateProgram();
            glAttachShader(program, vertex);
            glAttachShader(program, fragment);

            glLinkProgram(program);

            checkCompileErrors(program, true);

            glDeleteShader(vertex);
            glDeleteShader(fragment);

            glUseProgram(program);
            return program;
        }

        /// <summary>
        /// Creates a shader of the specified type from the given source string.
        /// </summary>
        /// <param name="type">An OpenGL enum for the shader type.</param>
        /// <param name="source">The source code of the shader.</param>
        /// <returns>The created shader. No error checking is performed for this basic example.</returns>
        private static uint CreateShader(int type, string source)
        {
            var shader = glCreateShader(type);
            glShaderSource(shader, source);
            glCompileShader(shader);
            checkCompileErrors(shader, false);
            return shader;
        }

        private static void checkCompileErrors(uint shader, bool isProgram)
        {
            if (!isProgram)
            {
                var errors = glGetShaderiv(shader, GL_COMPILE_STATUS, 1);
                if (errors[0] == 0)
                {
                    var message = glGetShaderInfoLog(shader, 1024);
                    throw new Exception("Error compiling: " + message);
                }
            }
            else
            {
                var errors = glGetProgramiv(shader, GL_LINK_STATUS, 1);
                if (errors[0] == 0)
                {
                    var message = glGetProgramInfoLog(shader, 1024);
                    throw new Exception("Error linking: " + message);
                }
            }
        }

        // 2 because screenspace is a [-1, -1], [1, 1] square
        const float KeyUnitWidth = 2f / 88f;

        /// <summary>
        /// Creates a VBO and VAO to store the vertices for a triangle.
        /// </summary>
        /// <param name="vao">The created vertex array object for the triangle.</param>
        /// <param name="vbo">The created vertex buffer object for the triangle.</param>
        private static unsafe List<VertexContainer> CreateVertices(int count)
        {
            var indexedVertices = Enumerable
                .Range(0, count)
                .Select(a => new float[]
                {
                    // front face
                    KeyUnitWidth, 1.0f, 0.0f, // top right
                    KeyUnitWidth, 0.0f, 0.0f, // bottom right
                    0.0f, 0.0f, 0.0f, // bottom left
                    0.0f, 1.0f, 0.0f,  // top left

                    // rear face
                    KeyUnitWidth, 1.0f, -1.0f, // top right
                    KeyUnitWidth, 0.0f, -1.0f, // bottom right
                    0.0f, 0.0f, -1.0f, // bottom left
                    0.0f, 1.0f, -1.0f  // top left
                })
                .ToList();

            var squareIndices = new uint[]
            {
                // rear face
                4, 5, 6,
                4, 7, 6,

                // top face
                7, 4, 0,
                7, 3, 0,

                // bottom face
                6, 5, 1,
                6, 2, 1,

                // left face
                7, 6, 2,
                7, 3, 2,

                // right face
                5, 1, 0,
                5, 4, 0,

                // front face
                0, 1, 2,
                0, 3, 2
            };



            var vertexArrays = glGenVertexArrays(indexedVertices.Count);
            var vertexBuffers = glGenBuffers(indexedVertices.Count);

            var VAO = glGenVertexArrays(indexedVertices.Count);
            var VBO = glGenBuffers(indexedVertices.Count);
            var EBO = glGenBuffers(indexedVertices.Count);

            var result = new List<VertexContainer>();

            for (var i = 0; i < indexedVertices.Count; ++i)
            {
                // 1. bind Vertex Array Object
                glBindVertexArray(VAO[i]);
                // 2. copy our vertices array in a vertex buffer for OpenGL to use

                glBindBuffer(GL_ARRAY_BUFFER, VBO[i]);
                var vertices = indexedVertices[i];
                fixed (float* v = &vertices[0])
                {
                    glBufferData(GL_ARRAY_BUFFER, sizeof(float) * vertices.Length, v, GL_STATIC_DRAW);
                }

                // 3. copy our index array in a element buffer for OpenGL to use
                glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, EBO[i]);
                fixed (uint* index = &squareIndices[0])
                {
                    glBufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(int) * squareIndices.Length, index, GL_STATIC_DRAW);
                }

                glVertexAttribPointer(0, 3, GL_FLOAT, false, 3 * sizeof(float), NULL);
                glEnableVertexAttribArray(0);

                // Reset buffer bindings
                glBindBuffer(GL_ARRAY_BUFFER, 0);
                glBindVertexArray(0);

                result.Add(new VertexContainer
                {
                    Color = GetRandomColor(),
                    ElementBuffer = EBO[i],
                    VertexArray = VAO[i],
                    VertexBuffer = VBO[i],
                });
            }

            return result;
        }

        private class VertexContainer
        {
            public uint ElementBuffer { get; set; }
            public uint VertexArray { get; set; }
            public uint VertexBuffer { get; set; }

            public float[] Color { get; set; } = default!;
        }
    }
}
