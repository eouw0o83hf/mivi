using System;
using System.Collections.Generic;
using System.IO;
using GLFW;
using Mivi.Core;
using static OpenGL.Gl;

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
        private const string TITLE = "Hello Triangle!";

        public static void EntryPoint(IMidiState state)
        {
            // Set context creation hints
            PrepareContext();
            // Create a window and shader program
            var window = CreateWindow(1024, 800);
            var program = CreateProgram();

            // Define a simple triangle
            var vertexContainers = CreateVertices(1);

            var location = glGetUniformLocation(program, "color");

            long n = 0;

            var black = new[] { 0f, 0f, 0f };

            while (!Glfw.WindowShouldClose(window))
            {
                // Swap fore/back framebuffers, and poll for operating system events.
                Glfw.SwapBuffers(window);
                Glfw.PollEvents();

                // Clear the framebuffer to defined background color
                glClear(GL_COLOR_BUFFER_BIT);

                ++n;

                glUseProgram(program);

                var keys = state.GetKeyStates();

                foreach (var (x, i) in vertexContainers.WithIndex())
                {
                    // 8 rows of 12 on screen for 96 total
                    // 88 keys on a piano
                    // 8 entry offset
                    var stateIndex = i + MidiNote.LowestPianoIndex;
                    if (stateIndex > keys.Length)
                    {
                        continue;
                    }

                    var color = keys[stateIndex] == 0 ? black : x.Color;

                    glUniform3f(location, color[0], color[1], color[2]);

                    glBindBuffer(GL_ARRAY_BUFFER, x.VertexBuffer);
                    glBindVertexArray(x.VertexArray);
                    glDrawArrays(GL_TRIANGLES, 0, 3);
                }
            }

            Glfw.Terminate();
        }

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

void main()
{
    gl_Position = vec4(pos.x, pos.y, pos.z, 1.0);
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
            return shader;
        }

        /// <summary>
        /// Creates a VBO and VAO to store the vertices for a triangle.
        /// </summary>
        /// <param name="vao">The created vertex array object for the triangle.</param>
        /// <param name="vbo">The created vertex buffer object for the triangle.</param>
        private static unsafe List<VertexContainer> CreateVertices(int verticeSet)
        {
            // 8 rows of 12
            var xCount = 8f;
            var yCount = 12f;
            var xSize = xCount / 2f;
            var ySize = yCount / 2f;
            var indexedVerticies = new List<float[]>();
            for (var i = 0; i < xCount; ++i)
            {
                for (var j = 0; j < yCount; ++j)
                {
                    indexedVerticies.Add(new float[]
                    {
                        // bottom left
                        (j / ySize) - 1f, (i / xSize) - 1f, 0f,
                        // bottom right
                        (j / ySize) - 1f, ((i + 1) / xSize) - 1f, 0f,
                        // top left
                        ((j + 1) / ySize) - 1f, (i / xSize) - 1f, 0f
                    });
                }
            }

            var vertexArrays = glGenVertexArrays(indexedVerticies.Count);
            var vertexBuffers = glGenBuffers(indexedVerticies.Count);

            var result = new List<VertexContainer>();

            for (var i = 0; i < indexedVerticies.Count; ++i)
            {
                glBindVertexArray(vertexArrays[i]);

                glBindBuffer(GL_ARRAY_BUFFER, vertexBuffers[i]);
                var vertices = indexedVerticies[i];
                fixed (float* v = &vertices[0])
                {
                    glBufferData(GL_ARRAY_BUFFER, sizeof(float) * vertices.Length, v, GL_STATIC_DRAW);
                }

                glVertexAttribPointer(0, 3, GL_FLOAT, false, 3 * sizeof(float), NULL);
                glEnableVertexAttribArray(0);

                result.Add(new VertexContainer
                {
                    VertexArray = vertexArrays[i],
                    VertexBuffer = vertexBuffers[i],
                    Color = GetRandomColor()
                });
            }

            return result;
        }

        private class VertexContainer
        {
            public uint VertexArray { get; set; }
            public uint VertexBuffer { get; set; }
            public float[] Color { get; set; } = default!;
        }
    }
}
