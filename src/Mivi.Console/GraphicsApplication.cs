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

        private const float ticksPerZUnit = 5f;
        private const float minVelocity = 0.1f;

        const float ticksStep = 0.1f;
        const float maxCameraMove = 0.7f;

        // map to a -1->1 continuum
        const float keyUnitWidth = 0.5f;
        const float keySpacing = 0.0065f;

        // given a continuous key index, determines
        // its position on the x axis in world space
        float keyIndexToXCoord(float i)
        {
            var offset = 44f * (keyUnitWidth + keySpacing);
            return (i * (keyUnitWidth + keySpacing)) - offset;
        }

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
            glDepthMask(true);
            glDepthFunc(GL_GEQUAL);

            Glfw.SetKeyCallback(window, KeyCallback);

            // Create present-key containers
            var circleResolution = 100;
            var cylinderVertexContainer = CreateCylinder(circleResolution);
            var cubeVertexContainer = CreateCube();

            var colorLocation = glGetUniformLocation(program, "color");
            var tickLocation = glGetUniformLocation(program, "ticks");

            // Projection matrix : 45° Field of View, 1:1 aspect ratio, z display range : 0.1 unit <-> 100 units
            var projectionMatrix = glm.perspective(
                glm.radians(45f),
                1f,     // aspect ratio
                0.0f,   // z min render
                100f    // z max render
            );

            var defaultCameraPosition = new vec3(0f, 40f, 44f);
            var currentCameraPosition = defaultCameraPosition;

            var cameraCenterZ = -10f;
            var defaultCameraCenter = new vec3(0, 17.5f, cameraCenterZ);
            var currentCameraCenter = defaultCameraCenter;
            var viewMatrix = glm.lookAt(
               currentCameraPosition,        // eye
               currentCameraCenter,      // center
               new vec3(0, 1, 0)                // up
           );

            var mvpMatrixLocation = glGetUniformLocation(program, "mvp");

            var colorProvider = new KeyColorProvider();
            // float so that we never have to worry about overflow
            // and we can make it a little more granular
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

                glUseProgram(program);
                glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

                var keys = _state.NoteVelocities;

                // if the sostenuto pedal is down and any keys are
                // pressed, zoom in
                vec3 nextCameraPosition = currentCameraPosition;
                vec3 nextCameraCenter = currentCameraCenter;
                if (_state.SostenutoPedalOn)
                {
                    if (keys.Count(a => a >= minVelocity) > 0)
                    {
                        var minKeyIndex = keys.TakeWhile(a => a < minVelocity).Count();
                        var maxKeyIndex = keys.Length - 1 - keys.Reverse().TakeWhile(a => a < minVelocity).Count();

                        var minIndex = minKeyIndex - MidiNote.LowestPianoIndex;
                        var maxIndex = maxKeyIndex - MidiNote.LowestPianoIndex;

                        var centerIndex = minIndex + ((maxIndex - minIndex) / 2f);

                        // view should be at the vertex of a 45/right triangle
                        // whose hypoteneuse is the far ends of the min/max bars
                        var minX = keyIndexToXCoord(minIndex);
                        // add spacing to the left of the bottom key
                        minX -= keySpacing;
                        // we want to add a border of the regular key spacing around
                        // the frame, and this method returns the bottom left position
                        // for the index. so, the beginning of the next index's position
                        // is the same as the end of the current one with margin
                        var maxX = keyIndexToXCoord(maxIndex + 1);
                        var width = maxX - minX;
                        var xPosition = minX + (width / 2f);

                        // 1:1 aspect ratio
                        var yMax = 1.0f;
                        var yMin = -1.0f;
                        var totalKeyboardWidth = 88f * keyUnitWidth + 87f * keySpacing;
                        // linearly interpolate between min and mix with key widths
                        var percentageCoverage = width / totalKeyboardWidth;
                        var yPosition = yMin + ((yMax - yMin) * percentageCoverage);

                        // 45 degree FoV. setup two right triangles whose shared
                        // leg is the vector from zPosition to the XY plane. the
                        // trig boils down to this factor
                        var widthFactor = 1.0f / (2.0f * (float)Math.Tan(Math.PI / 8f));
                        var zPosition = width * widthFactor;

                        nextCameraPosition = new vec3(xPosition, yPosition, zPosition);
                        nextCameraCenter = new vec3(xPosition, yPosition + 1f, cameraCenterZ);
                    }
                }
                else
                {
                    nextCameraPosition = defaultCameraPosition;
                    nextCameraCenter = defaultCameraCenter;
                }

                // smooth transitions
                // for some reasons these datatypes don't deal with being
                // mutated inside sets. this whole thing needs to be a
                // first-class pipeline or state manager
                for (var i = 0; i < 3; ++i)
                {
                    var difference = currentCameraPosition[i] - nextCameraPosition[i];

                    if (difference > maxCameraMove)
                    {
                        nextCameraPosition[i] = currentCameraPosition[i] - maxCameraMove;
                    }
                    else if (difference < -maxCameraMove)
                    {
                        nextCameraPosition[i] = currentCameraPosition[i] + maxCameraMove;
                    }
                }

                for (var i = 0; i < 3; ++i)
                {
                    var difference = currentCameraCenter[i] - nextCameraCenter[i];

                    if (difference > maxCameraMove)
                    {
                        nextCameraCenter[i] = currentCameraCenter[i] - maxCameraMove;
                    }
                    else if (difference < -maxCameraMove)
                    {
                        nextCameraCenter[i] = currentCameraCenter[i] + maxCameraMove;
                    }
                }

                currentCameraPosition = nextCameraPosition;
                currentCameraCenter = nextCameraCenter;

                viewMatrix = glm.lookAt(
                    currentCameraPosition,
                    currentCameraCenter,
                    new vec3(0, 1, 0)  // up
                );

                // Draw historical notes first since they're in the back
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

                    glBindVertexArray(cubeVertexContainer.VertexArray);

                    // top two of the rightmost column
                    var translateMatrix = new mat4(1.0f);
                    translateMatrix[3, 0] = keyIndexToXCoord(adjustedIndex); // x position
                    translateMatrix[3, 1] = -1; // y position
                    translateMatrix[3, 2] = -pastNote.TicksSinceKeyUp / ticksPerZUnit; // z position

                    var rotationMatrix = new mat4(1.0f);

                    var scaleMatrix = new mat4(1.0f);
                    scaleMatrix[0, 0] = keyUnitWidth; // x scale
                    scaleMatrix[1, 1] = scaleVolume(pastNote.Velocity); // y scale
                    scaleMatrix[2, 2] = pastNote.Length / ticksPerZUnit;

                    var modelMatrix = translateMatrix * rotationMatrix * scaleMatrix;

                    var mvpMatrix = projectionMatrix * viewMatrix * modelMatrix;

                    glUniformMatrix4fv(mvpMatrixLocation, 1, false, mvpMatrix.to_array());

                    glDrawElements(GL_TRIANGLES, 6 * 3 * 3, GL_UNSIGNED_INT, NULL);
                }

                // Draw current notes
                for (var i = 0; i < 88; ++i)
                {
                    var adjustedIndex = i + MidiNote.LowestPianoIndex;

                    var velocity = keys[adjustedIndex];
                    if (velocity < minVelocity)
                    {
                        continue;
                    }

                    var color = colorProvider.GetColor(adjustedIndex);

                    glUniform3f(colorLocation, color[0], color[1], color[2]);

                    glBindVertexArray(cubeVertexContainer.VertexArray);

                    // top two of the rightmost column
                    var translateMatrix = new mat4(1.0f);
                    // * 1.25 to add a little space between adjacent keys
                    translateMatrix[3, 0] = keyIndexToXCoord(i); // x position
                    translateMatrix[3, 1] = -1; // y position

                    var rotationMatrix = new mat4(1.0f);

                    var scaleMatrix = new mat4(1.0f);
                    scaleMatrix[0, 0] = keyUnitWidth; // x scale
                    scaleMatrix[1, 1] = scaleVolume(velocity); // y scale
                    scaleMatrix[2, 2] = _state.NoteLengths[adjustedIndex] / ticksPerZUnit;

                    var modelMatrix = translateMatrix * rotationMatrix * scaleMatrix;

                    var mvpMatrix = projectionMatrix * viewMatrix * modelMatrix;

                    glUniformMatrix4fv(mvpMatrixLocation, 1, false, mvpMatrix.to_array());

                    glDrawElements(GL_TRIANGLES, 6 * 3 * 3, GL_UNSIGNED_INT, NULL);
                    // example for rendering cylinder
                    // glDrawElements(GL_TRIANGLES, circleResolution * 3 * 2, GL_UNSIGNED_INT, NULL);
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
        private static float scaleVolume(float midiVelocity) => midiVelocity / 15f;

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
    result = vec4(r, g, b, 1.0);
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

        private static unsafe VertexContainer CreateCylinder(int cylinderCircleVertexCount)
        {
            // describe a circle around a point
            // closer face first, then further face
            float radiansPerVertex = 2f * (float)Math.PI / cylinderCircleVertexCount;

            var cylinderVertices = new float[cylinderCircleVertexCount * 2 * 3];
            var cylinderIndeces = new uint[cylinderCircleVertexCount * 2 * 3];

            var baseIndex = 0;
            for (var z = 0; z < 2; ++z)
            {
                for (var i = 0; i < cylinderCircleVertexCount; ++i)
                {
                    // x
                    cylinderVertices[baseIndex] = (float)Math.Cos(i * radiansPerVertex);
                    // y
                    cylinderVertices[baseIndex + 1] = (float)Math.Sin(i * radiansPerVertex);
                    // z
                    cylinderVertices[baseIndex + 2] = -z;

                    // current node
                    cylinderIndeces[baseIndex] = (uint)(cylinderCircleVertexCount * z + i);
                    // "next" node in the circle with wraparound
                    var nextI = ((cylinderCircleVertexCount * z) + i + 1) % cylinderCircleVertexCount;
                    cylinderIndeces[baseIndex + 1] = (uint)nextI;
                    var otherI = z == 0 ? i : nextI;
                    cylinderIndeces[baseIndex + 2] = (uint)((otherI + cylinderCircleVertexCount) % (cylinderCircleVertexCount * 2));


                    baseIndex += 3;
                }
            }

            return CreateShapeCore(cylinderVertices, cylinderIndeces);
        }

        /// <summary>
        /// Creates a VBO and VAO to store the vertices for a triangle.
        /// </summary>
        /// <param name="vao">The created vertex array object for the triangle.</param>
        /// <param name="vbo">The created vertex buffer object for the triangle.</param>
        private static unsafe VertexContainer CreateCube()
        {
            var vertices = new float[]
                {
                    // front face
                    1.0f, 1.0f, 0.0f, // top right
                    1.0f, 0.0f, 0.0f, // bottom right
                    0.0f, 0.0f, 0.0f, // bottom left
                    0.0f, 1.0f, 0.0f,  // top left

                    // rear face
                    1.0f, 1.0f, -1.0f, // top right
                    1.0f, 0.0f, -1.0f, // bottom right
                    0.0f, 0.0f, -1.0f, // bottom left
                    0.0f, 1.0f, -1.0f  // top left
                };

            var squareIndices = new uint[]
            {
                // bottom face
                6, 5, 1,
                6, 2, 1,

                // rear face
                4, 5, 6,
                4, 7, 6,

                // left face
                7, 6, 2,
                7, 3, 2,

                // right face
                5, 1, 0,
                5, 4, 0,

                // top face
                7, 4, 0,
                7, 3, 0,

                // front face
                0, 1, 2,
                0, 3, 2
            };

            return CreateShapeCore(vertices, squareIndices);
        }

        private static unsafe VertexContainer CreateShapeCore(float[] vertices, uint[] indeces)
        {
            var vertexArrays = glGenVertexArrays(vertices.Length);
            var vertexBuffers = glGenBuffers(vertices.Length);

            var VAO = glGenVertexArray();
            var VBO = glGenBuffer();
            var EBO = glGenBuffer();

            // 1. bind Vertex Array Object
            glBindVertexArray(VAO);
            // 2. copy our vertices array in a vertex buffer for OpenGL to use

            glBindBuffer(GL_ARRAY_BUFFER, VBO);
            fixed (float* v = &vertices[0])
            {
                glBufferData(GL_ARRAY_BUFFER, sizeof(float) * vertices.Length, v, GL_STATIC_DRAW);
            }

            // 3. copy our index array in a element buffer for OpenGL to use
            glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, EBO);
            fixed (uint* index = &indeces[0])
            {
                glBufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(uint) * indeces.Length, index, GL_STATIC_DRAW);
            }

            glVertexAttribPointer(0, 3, GL_FLOAT, false, 3 * sizeof(float), NULL);
            glEnableVertexAttribArray(0);

            // Reset buffer bindings
            glBindBuffer(GL_ARRAY_BUFFER, 0);
            glBindVertexArray(0);

            return new VertexContainer
            {
                Color = GetRandomColor(),
                ElementBuffer = EBO,
                VertexArray = VAO,
                VertexBuffer = VBO,
            };
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
