using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Szeminarium1
{
    internal static class Program
    {
        private static IWindow graphicWindow;

        private static GL Gl;

        private static uint program;

        private static void CheckGLError(string stepName)
        {
            GLEnum error = Gl.GetError();
            if (error != GLEnum.NoError)
            {
                Console.WriteLine($"[OpenGL Hiba] Itt történt hiba: {stepName} -> Hibakód: {error}");
            }
        }

        //layout (location = 2) in vec4 vCol -ra csereltem le a 18-as sort 
        // es a forma amit eddig szinesen kirajzolt fekete lett
        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;

		out vec4 outCol;
        
        void main()
        {
			outCol = vCol;
            gl_Position = vec4(vPos.x * 0.7 - 0.1, vPos.y * 0.7, vPos.z, 1.0);
        }
        ";


        private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
		
		in vec4 outCol;

        void main()
        {
            FragColor = outCol;
        }
        ";

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "1. szeminárium - háromszög";
            // kicsereltem: windowOptions.Size = new Silk.NET.Maths.Vector2D<int>(200, 200); a 48-as sort
            // kisebb lett az ablak merete
            windowOptions.Size = new Silk.NET.Maths.Vector2D<int>(800, 800);

            graphicWindow = Window.Create(windowOptions);

            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;

            graphicWindow.Run();
        }

        private static void GraphicWindow_Load()
        {
            // egszeri beallitasokat
            //Console.WriteLine("Loaded");

            Gl = graphicWindow.CreateOpenGL();

            //Gl.ClearColor(System.Drawing.Color.Black);
            //fekete lett a hattere
            Gl.ClearColor(System.Drawing.Color.White);

            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);
            CheckGLError("Vertex es Fragment shader hibaellenorzes");

            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
            CheckGLError("Program hibaellenorzese");

            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }

        }

        private static void GraphicWindow_Update(double deltaTime)
        {
            // NO GL
            // make it threadsave
            //Console.WriteLine($"Update after {deltaTime} [s]");
        }

        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            //Console.WriteLine($"Render after {deltaTime} [s]");

            Gl.Clear(ClearBufferMask.ColorBufferBit);

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            //valtoztattam az vertexArray ertekeit
            /*float[] vertexArray = new float[] {
                -0.6f, -0.6f, 0.0f,
                +0.5f, -0.5f, 0.0f,
                 0.0f, +0.5f, 0.0f,
                 0f, 0f, 0f
            };*/

            float[] vertexArray = new float[] {
                0.0f, 0.0f, 0.0f,
                1.0f, 0.5f, 0.0f,
                0.0f, 1.0f, 0.0f,
               -1.0f, 0.5f, 0.0f,

                0.0f, 0.0f, 0.0f,
                1.0f, 0.5f, 0.0f,
                1.0f, -0.6f,0.0f,
                0.0f, -1.2f, 0.0f,

                0.0f, 0.0f, 0.0f,
                0.0f, -1.2f, 0.0f,
               -1.0f, -0.6f, 0.0f,
               -1.0f, 0.5f, 0.0f,

                0.31f, 0.19f, 0.0f,
               -0.65f, 0.65f, 0.0f,

               -0.35f, 0.83f, 0.0f,
                0.67f, 0.35f, 0.0f,

               -0.37f, 0.15f, 0.0f,
                0.65f, 0.69f, 0.0f,

                -0.74f, 0.3f, 0.0f,
                 0.34f, 0.84f,0.0f,

                -0.37f, 0.15f, 0.0f,
                -0.37f, -0.96f, 0.0f,

                -0.74f, 0.3f, 0.0f,
                -0.74f, -0.75f, 0.0f,

                -1.0f, 0.1f, 0.0f,
                 0.0f,-0.45f, 0.0f,

                -1.0f, -0.25f, 0.0f,
                 0.0f, -0.85f, 0.0f,

                 0.31f, 0.19f, 0.0f,
                 0.32f,-1.0f, 0.0f,

                 0.67f, 0.35f, 0.0f,
                 0.67f, -0.8f, 0.0f,

                 1.0f, 0.1f, 0.0f,
                 0.0f, -0.45f, 0.0f,

                0.0f, -0.85f, 0.0f,
                1.0f, -0.27f, 0.0f

            };

            //kicsereltem az 1-eseket 2-esekre az arrayban es vilagos szinu lett a forma
            /*                    
                float[] colorArray = new float[] {
                    2.0f, 0.0f, 0.0f, 2.0f,
                    0.0f, 2.0f, 0.0f, 2.0f,
                    0.0f, 0.0f, 2.0f, 2.0f,
                    2.0f, 0.0f, 0.0f, 2.0f,
                };
            */
            float[] colorArray = new float[] {
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,

                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f
            };

            uint[] indexArray = new uint[] {
                0, 1, 2,
                0, 2, 3,

                4, 5, 6,
                4, 6 ,7,

                8, 9, 10,
                8, 10 ,11,

                12, 13,
                14, 15,
                16, 17,
                18, 19,
                20, 21,
                22, 23,
                24, 25,
                26, 27,
                28, 29,
                30, 31,
                32, 33,
                34, 35
            };

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(0);
            CheckGLError("Csucsok hibaellenorzes");

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);
            CheckGLError("Szinex beallitasanal hibaellenorzes");

            uint indices = Gl.GenBuffer();
            //felcsereltem a ket parancs sorrendjet es igy mar nem jelenik meg az alakzat
            //Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);
            //Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);

            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);

            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            Gl.UseProgram(program);

            Gl.DrawElements(GLEnum.Triangles, 18, GLEnum.UnsignedInt, null); // we used element buffer
            Gl.DrawElements(GLEnum.Lines, 24, GLEnum.UnsignedInt, (void*)(18 * sizeof(uint)));
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);

            // always unbound the vertex buffer first, so no halfway results are displayed by accident
            Gl.DeleteBuffer(vertices);
            Gl.DeleteBuffer(colors);
            Gl.DeleteBuffer(indices);
            Gl.DeleteVertexArray(vao);
            CheckGLError("Torlesnel hibaellenorzes");
        }
    }
}
