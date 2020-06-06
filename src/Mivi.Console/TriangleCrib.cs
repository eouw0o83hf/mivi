using System;
using System.Collections.Generic;
using System.IO;
using GLFW;
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

        public static void EntryPoint(string[] args)
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

            while (!Glfw.WindowShouldClose(window))
            {
                // Swap fore/back framebuffers, and poll for operating system events.
                Glfw.SwapBuffers(window);
                Glfw.PollEvents();

                // Clear the framebuffer to defined background color
                glClear(GL_COLOR_BUFFER_BIT);

                ++n;

                glUseProgram(program);

                foreach (var x in vertexContainers)
                {
                    if (n % 60 == 0)
                    {
                        x.Color = GetRandomColor();
                    }
                    glUniform3f(location, x.Color[0], x.Color[1], x.Color[2]);

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

        /// <summary>
        /// Creates an extremely basic shader program that is capable of displaying a triangle on screen.
        /// </summary>
        /// <returns>The created shader program. No error checking is performed for this basic example.</returns>
        private static uint CreateProgram()
        {
            var vertex = CreateShader(GL_VERTEX_SHADER, File.ReadAllText("./triangle.vert"));
            var fragment = CreateShader(GL_FRAGMENT_SHADER, File.ReadAllText("./triangle.frag"));

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

            var vertices1 = new[] {
                -0.5f, -0.5f, 0.0f,
                0.5f, -0.5f, 0.0f,
                0.0f,  0.5f, 0.0f
            };
            var vertices2 = new[] {
                0.0f, 0.6f, 0.0f,
                0.9f, 0.9f, 0.0f,
                0.9f, -0.2f, 0.0f
            };

            var vertices3 = new[] {
                -0.9f, -0.9f, 0.0f,
                -0.9f, -0.6f, 0.0f,
                -0.8f, -0.8f, 0.0f,
            };
            var vertices4 = new[] {
                -0.9f, 0.9f, 0.0f,
                -0.9f, 0.6f, 0.0f,
                -0.8f, 0.8f, 0.0f,
            };

            var verticeses = new[]
            {
                vertices1, vertices2, vertices3, vertices4
            };

            var vertexArrays = glGenVertexArrays(4);
            var vertexBuffers = glGenBuffers(4);

            var result = new List<VertexContainer>();

            for (var i = 0; i < 4; ++i)
            {
                glBindVertexArray(vertexArrays[i]);

                glBindBuffer(GL_ARRAY_BUFFER, vertexBuffers[i]);
                var vertices = verticeses[i];
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
