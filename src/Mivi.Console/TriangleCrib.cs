using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GLFW;
using GlmNet;
using Mivi.Core;
using static OpenGL.Gl;
using Exception = System.Exception;

namespace Mivi.Console
{
    /// <summary>
    /// Basic GLFW example to create a window with an OpenGL 3.3 Core profile context and draw a triangle.
    /// </summary>
    public class TriangleProgram
    {
        /// <summary>
        /// Obligatory name for your first OpenGL example program.
        /// </summary>
        private const string TITLE = "eouw0o83hf MIVI";

        public static unsafe void EntryPoint(SharedState state)
        {
            // Set context creation hints
            PrepareContext();
            // Create a window and shader program
            var window = CreateWindow(1024, 800);
            var program = CreateProgram();

            // Define a simple triangle
            var vertexContainers = CreateVertices(1);

            var colorLocation = glGetUniformLocation(program, "color");

            var black = new[] { 0f, 0f, 0f };

            // Projection matrix : 45° Field of View, 4:3 ratio, display range : 0.1 unit <-> 100 units
            // glm::mat4 Projection = glm::perspective(glm::radians(45.0f), (float) width / (float)height, 0.1f, 100.0f);
            var projectionMatrix = glm.perspective(
                glm.radians(45f),
                1f, 0.1f, 100f
            );

            var viewMatrix = glm.lookAt(
               new vec3(0f, 1f, 2.5f),
               new vec3(0, 0.2f, 0),
               new vec3(0, 1, 0)
           );

            var mvpMatrixLocation = glGetUniformLocation(program, "mvp");

            while (!Glfw.WindowShouldClose(window))
            {
                // Swap fore/back framebuffers, and poll for operating system events.
                Glfw.SwapBuffers(window);
                Glfw.PollEvents();

                // Clear the framebuffer to defined background color
                glClear(GL_COLOR_BUFFER_BIT);

                glUseProgram(program);

                var keys = state.NoteVelocities;

                foreach (var (x, i) in vertexContainers.WithIndex())
                {
                    var velocity = keys[i + MidiNote.LowestPianoIndex];
                    var defaultColor = new[]
                    {
                        .8f - (i * 0.3f / 88f),
                        0.5f - (i / 176f),
                        (i / 100f)
                    };
                    var color = velocity <= 0.001f ? black : defaultColor;

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
                    scaleMatrix[2, 2] = scaleVolume(velocity); // z scale

                    var modelMatrix = translateMatrix * rotationMatrix * scaleMatrix;

                    var mvpMatrix = projectionMatrix * viewMatrix * modelMatrix;

                    glUniformMatrix4fv(mvpMatrixLocation, 1, false, mvpMatrix.to_array());

                    glDrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, NULL);
                }
            }

            Glfw.Terminate();
        }

        // This should probably be logarithmic
        private static float scaleVolume(float midiVelocity) => midiVelocity / 64f;

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
            Glfw.WindowHint(Hint.ClientApi, ClientApi.OpenGL);
            Glfw.WindowHint(Hint.ContextVersionMajor, 3);
            Glfw.WindowHint(Hint.ContextVersionMinor, 3);
            Glfw.WindowHint(Hint.OpenglProfile, Profile.Core);
            Glfw.WindowHint(Hint.Doublebuffer, true);
            Glfw.WindowHint(Hint.Decorated, true);
            Glfw.WindowHint(Hint.OpenglForwardCompatible, true);
        }

        /// <summary>
        /// Creates and returns a handle to a GLFW window with a current OpenGL context.
        /// </summary>
        /// <param name="width">The width of the client area, in pixels.</param>
        /// <param name="height">The height of the client area, in pixels.</param>
        /// <returns>A handle to the created window.</returns>
        private static Window CreateWindow(int width, int height)
        {
            // Create window, make the OpenGL context current on the thread, and import graphics functions
            var window = Glfw.CreateWindow(width, height, TITLE, Monitor.None, Window.None);
            Glfw.MakeContextCurrent(window);
            Import(Glfw.GetProcAddress);

            // Center window
            var screen = Glfw.PrimaryMonitor.WorkArea;
            var x = (screen.Width - width) / 2;
            var y = (screen.Height - height) / 2;
            Glfw.SetWindowPosition(window, x, y);

            return window;
        }

        private const string TriangleVertexShader = @"
#version 330 core

layout (location = 0) in vec3 pos;

uniform mat4 mvp;

void main()
{
    gl_Position = mvp * vec4(pos, 1.0);
}
";

        private const string TriangleFragmentShader = @"
#version 330 core
out vec4 result;

uniform vec3 color;

void main()
{
    result = vec4(color, 1.0);
}
";

        /// <summary>
        /// Creates an extremely basic shader program that is capable of displaying a triangle on screen.
        /// </summary>
        /// <returns>The created shader program. No error checking is performed for this basic example.</returns>
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
        private static unsafe List<VertexContainer> CreateVertices(int verticeSet)
        {
            var indexedVertices = Enumerable
                .Range(0, 88)
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
                // front face
                0, 1, 2,    // triangle 1
                0, 3, 2,    // triangle 2

                // rear face
                4, 5, 6,
                4, 7, 6,

                // top face
                0, 4, 7,
                0, 3, 7,

                // bottom face
                1, 5, 6,
                1, 2, 6,

                // left face
                2, 3, 6,
                2, 7, 6,

                // right face
                0, 1, 5,
                0, 4, 5
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
