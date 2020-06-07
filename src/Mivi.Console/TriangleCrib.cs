using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GLFW;
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
        private const string TITLE = "Hello Triangle!";

        public static unsafe void EntryPoint(IMidiState state)
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

            var translateLocation = glGetUniformLocation(program, "translate");
            // identity, just set x and y
            var translateMatrix = new[]
            {
                1.0f, 0f, 0f, 0f,
                0f, 1.0f, 0f, 0f,
                0f, 0f, 1.0f, 0f,
                0f, 0f, 0f, 1.0f
            };

            var transformLocation = glGetUniformLocation(program, "transform");
            var transformationMatrix = new[]
            {
                0.1f, 0f, 0f, 0f,
                0f, 0.1f, 0f, 0f,
                0f, 0f, 0.1f, 0f,
                0f, 0f, 0f, 1.0f
            };
            glUniformMatrix4fv(transformLocation, 1, false, transformationMatrix);

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


                    glBindVertexArray(x.VertexArray);

                    var xIndex = i % 12;
                    var yIndex = i / 12;
                    var xUnit = (float)(xIndex - 6) / 6f;
                    var yUnit = (float)(yIndex - 4) / 4f;

                    translateMatrix[12] = xUnit; // x position
                    translateMatrix[13] = yUnit; // y position
                    glUniformMatrix4fv(translateLocation, 1, false, translateMatrix);


                    glDrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, NULL);
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

uniform mat4 transform;
uniform mat4 translate;

void main()
{
    gl_Position = translate * transform * vec4(pos, 1.0);
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

        /// <summary>
        /// Creates a VBO and VAO to store the vertices for a triangle.
        /// </summary>
        /// <param name="vao">The created vertex array object for the triangle.</param>
        /// <param name="vbo">The created vertex buffer object for the triangle.</param>
        private static unsafe List<VertexContainer> CreateVertices(int verticeSet)
        {

            // var indexedVertices = Enumerable
            //     .Range(0, 8 * 12)
            //     .Select(a => new float[]
            //     {
            //         1.0f, 1.0f, 0.0f, // top right
            //         1.0f, 0.0f, 0.0f, // bottom right
            //         0.0f, 0.0f, 0.0f, // bottom left
            //         0.0f, 1.0f, 0.0f  // top left
            //     })
            //     .ToList();


            // 8 rows of 12
            var xCount = 8f;
            var yCount = 12f;
            var xSize = xCount / 2f;
            var ySize = yCount / 2f;

            var indexedVertices = new List<float[]>();

            for (var i = 0; i < xCount; ++i)
            {
                for (var j = 0; j < yCount; ++j)
                {
                    indexedVertices.Add(new float[]
                    {
                        // bottom left
                        (j / ySize) - 1f, (i / xSize) - 1f, 0f,
                        // bottom right
                        (j / ySize) - 1f, ((i + 1) / xSize) - 1f, 0f,
                        // top right
                        ((j + 1) / ySize) - 1f, ((i + 1) / xSize) - 1f, 0f,
                        // top left
                        ((j + 1) / ySize) - 1f, (i / xSize) - 1f, 0f
                    });
                }
            }

            var squareIndices = new uint[]
            {
                0, 1, 3,    // triangle 1
                1, 2, 3     // triangle 2
            };

            var vertexArrays = glGenVertexArrays(indexedVertices.Count);
            var vertexBuffers = glGenBuffers(indexedVertices.Count);

            var VAO = glGenVertexArrays(indexedVertices.Count);
            var VBO = glGenBuffers(indexedVertices.Count);
            var EBO = glGenBuffers(indexedVertices.Count);

            var result = new List<VertexContainer>();

            // https://learnopengl.com/Getting-started/Hello-Triangle
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