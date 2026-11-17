using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System.Dynamic;
using System.Numerics;
using System.Reflection;
using Szeminarium;

namespace GrafikaSzeminarium
{
    internal class Program
    {
        private static IWindow graphicWindow;

        private static GL Gl;

        private static ImGuiController imGuiController;

        private static ModelObjectDescriptor cube;

        private static CameraDescriptor camera = new CameraDescriptor();

        private static CubeArrangementModel cubeArrangementModel = new CubeArrangementModel();

        private static uint program;
        private static int shadingMode;
        private static uint programPhong;
        private static uint programGouraud;
        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private const string LightColorVariableName = "uLightColor";
        private const string LightPositionVariableName = "uLightPos";
        private const string ViewPositionVariableName = "uViewPos";

        private const string ShinenessVariableName = "uShininess";

        private static float shininess = 50;

        private static ModelObjectDescriptor flat;
        private static ModelObjectDescriptor smooth;

        private const int BoardCount = 18;
        private const float h = 2f;
        private const float sz = 1f;
        private static readonly float StepAngle = 2f * MathF.PI / BoardCount; // 360/18 = 20 fok
        private static readonly float HalfStepAngle = StepAngle / 2f; // 10 fok

        private static readonly float Befogo = (sz / 2f) / MathF.Tan(HalfStepAngle); // 0.5/tan(10)
        private static readonly float Radius = (sz / 2f) / MathF.Sin(HalfStepAngle); // 0.5/sin(10)


        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Grafika szeminárium";
            windowOptions.Size = new Silk.NET.Maths.Vector2D<int>(700, 700);

            graphicWindow = Window.Create(windowOptions);

            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;
            graphicWindow.Closing += GraphicWindow_Closing;

            graphicWindow.Run();
        }

        private static void GraphicWindow_Closing()
        {
            flat.Dispose();
            smooth.Dispose();
            //cube.Dispose();
            Gl.DeleteProgram(program);
        }

        private static void GraphicWindow_Load()
        {
            Gl = graphicWindow.CreateOpenGL();

            var inputContext = graphicWindow.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            graphicWindow.FramebufferResize += size => Gl.Viewport(size);
            imGuiController = new ImGuiController(Gl, graphicWindow, inputContext);

            float[] szin = new float[] { 0.75f, 0.5f, 0.25f, 1f };
            flat = ModelObjectDescriptor.CreateRectangle(Gl, sz, h, false, HalfStepAngle, szin);
            smooth = ModelObjectDescriptor.CreateRectangle(Gl, sz, h, true, HalfStepAngle, szin);

            Gl.ClearColor(System.Drawing.Color.White);
            Gl.CullFace(TriangleFace.Back);

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);


            uint vshaderPhong = Gl.CreateShader(ShaderType.VertexShader);
            uint fshaderPhong = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshaderPhong, GetEmbeddedResourceAsString("Shaders.VertexShader.vert"));
            Gl.CompileShader(vshaderPhong);
            Gl.GetShader(vshaderPhong, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshaderPhong));

            Gl.ShaderSource(fshaderPhong, GetEmbeddedResourceAsString("Shaders.FragmentShader.frag"));
            Gl.CompileShader(fshaderPhong);
            Gl.GetShader(fshaderPhong, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
                throw new Exception("Fragment shader failed to compile: " + Gl.GetShaderInfoLog(fshaderPhong));

            programPhong = Gl.CreateProgram();
            Gl.AttachShader(programPhong, vshaderPhong);
            Gl.AttachShader(programPhong, fshaderPhong);
            Gl.LinkProgram(programPhong);

            Gl.DetachShader(programPhong, vshaderPhong);
            Gl.DetachShader(programPhong, fshaderPhong);
            Gl.DeleteShader(vshaderPhong);
            Gl.DeleteShader(fshaderPhong);

            Gl.GetProgram(programPhong, GLEnum.LinkStatus, out var statusPhong);
            if (statusPhong == 0)
            {
                throw new Exception($"Error linking Phong shader: {Gl.GetProgramInfoLog(programPhong)}");
            }

            uint vshaderGouraud = Gl.CreateShader(ShaderType.VertexShader);
            uint fshaderGouraud = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshaderGouraud, GetEmbeddedResourceAsString("Shaders.Gouraud.vert"));
            Gl.CompileShader(vshaderGouraud);
            Gl.GetShader(vshaderGouraud, ShaderParameterName.CompileStatus, out int vStatusG);
            if (vStatusG != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshaderGouraud));


            Gl.ShaderSource(fshaderGouraud, GetEmbeddedResourceAsString("Shaders.Gouraud.frag"));
            Gl.CompileShader(fshaderGouraud);
            Gl.GetShader(fshaderGouraud, ShaderParameterName.CompileStatus, out int fStatusG);
            if (fStatusG != (int)GLEnum.True)
                throw new Exception("Fragment shader failed to compile: " + Gl.GetShaderInfoLog(fshaderGouraud));


            programGouraud = Gl.CreateProgram();
            Gl.AttachShader(programGouraud, vshaderGouraud);
            Gl.AttachShader(programGouraud, fshaderGouraud);
            Gl.LinkProgram(programGouraud);

            Gl.DetachShader(programGouraud, vshaderGouraud);
            Gl.DetachShader(programGouraud, fshaderGouraud);
            Gl.DeleteShader(vshaderGouraud);
            Gl.DeleteShader(fshaderGouraud);

            Gl.GetProgram(programGouraud, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(programGouraud)}");
            }

            program = programPhong;
        }

        private static string GetEmbeddedResourceAsString(string resourceRelativePath)
        {
            string resourceFullPath = Assembly.GetExecutingAssembly().GetName().Name + "." + resourceRelativePath;

            using (var resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceFullPath))
            using (var resStreamReader = new StreamReader(resStream))
            {
                var text = resStreamReader.ReadToEnd();
                return text;
            }
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.Left:
                    camera.DecreaseZYAngle();
                    break;
                case Key.Right:
                    camera.IncreaseZYAngle();
                    break;
                case Key.Down:
                    camera.IncreaseDistance();
                    break;
                case Key.Up:
                    camera.DecreaseDistance();
                    break;
                case Key.U:
                    camera.IncreaseZXAngle();
                    break;
                case Key.D:
                    camera.DecreaseZXAngle();
                    break;
                case Key.Space:
                    cubeArrangementModel.AnimationEnabled = !cubeArrangementModel.AnimationEnabled;
                    break;
            }
        }

        private static void GraphicWindow_Update(double deltaTime)
        {
            // NO OpenGL
            // make it threadsafe
            cubeArrangementModel.AdvanceTime(deltaTime);
        }

        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            imGuiController.Update((float)deltaTime);
            var fb = graphicWindow.FramebufferSize;
            Gl.Viewport(0, 0, (uint)fb.X, (uint)fb.Y);
            program = (shadingMode == 0) ? programPhong : programGouraud;

            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            var camPos = new Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z);
            SetUniform3(LightColorVariableName, new Vector3(1f, 1f, 1f));
            SetUniform3(LightPositionVariableName, camPos);
            SetUniform3(ViewPositionVariableName, camPos);
            SetUniform1(ShinenessVariableName, shininess);

            // SetUniform3(LightColorVariableName, new Vector3(1f, 1f, 1f));
            // SetUniform3(LightPositionVariableName, new Vector3(0f, 1.2f, 0f));
            // SetUniform3(ViewPositionVariableName, new Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z));
            // SetUniform1(ShinenessVariableName, shininess);

            var viewMatrix = Matrix4X4.CreateLookAt(camera.Position, camera.Target, camera.UpVector);
            SetMatrix(viewMatrix, ViewMatrixVariableName);

            float aspect = (float)fb.X / fb.Y;
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)(Math.PI / 3), aspect, 0.1f, 100f);
            SetMatrix(projectionMatrix, ProjectionMatrixVariableName);


            float xOffset = Radius + 0.6f;
            DrawBarrel(flat, -xOffset);
            DrawBarrel(smooth, xOffset);

            // var modelMatrixCenterCube = Matrix4X4.CreateScale((float)cubeArrangementModel.CenterCubeScale);
            // SetModelMatrix(modelMatrixCenterCube);
            // DrawModelObject(cube);

            // Matrix4X4<float> diamondScale = Matrix4X4.CreateScale(0.25f);
            // Matrix4X4<float> rotx = Matrix4X4.CreateRotationX((float)Math.PI / 4f);
            // Matrix4X4<float> rotz = Matrix4X4.CreateRotationZ((float)Math.PI / 4f);
            // Matrix4X4<float> roty = Matrix4X4.CreateRotationY((float)cubeArrangementModel.DiamondCubeLocalAngle);
            // Matrix4X4<float> trans = Matrix4X4.CreateTranslation(1f, 1f, 0f);
            // Matrix4X4<float> rotGlobalY = Matrix4X4.CreateRotationY((float)cubeArrangementModel.DiamondCubeGlobalYAngle);
            // Matrix4X4<float> dimondCubeModelMatrix = diamondScale * rotx * rotz * roty * trans * rotGlobalY;
            // SetModelMatrix(dimondCubeModelMatrix);
            // DrawModelObject(cube);

            ImGuiNET.ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiNET.ImGuiCond.FirstUseEver);
            ImGuiNET.ImGui.SetNextWindowSize(new Vector2(800, 520), ImGuiNET.ImGuiCond.FirstUseEver);
            ImGuiNET.ImGui.Begin("Lighting");
            ImGuiNET.ImGui.SliderFloat("Shininess", ref shininess, 5, 100);
            ImGuiNET.ImGui.Separator();
            ImGuiNET.ImGui.Text("Shading Model:");
            ImGuiNET.ImGui.RadioButton("Phong", ref shadingMode, 0);
            ImGuiNET.ImGui.RadioButton("Gouraud", ref shadingMode, 1);
            ImGuiNET.ImGui.End();

            imGuiController.Render();
        }

        private static unsafe void DrawBarrel(ModelObjectDescriptor board, float xOffset)
        {
            Matrix4X4<float> moveOut = Matrix4X4.CreateTranslation(0f, 0f, Befogo);
            Matrix4X4<float> moveBarrel = Matrix4X4.CreateTranslation(xOffset, 0f, 0f);

            for (int i = 0; i < BoardCount; i++)
            {
                Matrix4X4<float> rot = Matrix4X4.CreateRotationY(i * StepAngle);
                //eloszor ki kell tolnunk a Z tengely menten a Befogo-val, majd elforgatni az Y tengely korul
                //majd eltoljuk
                SetModelMatrix(moveOut * rot * moveBarrel);
                DrawModelObject(board);
            }
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            SetMatrix(modelMatrix, ModelMatrixVariableName);

            // set also the normal matrix
            int location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{NormalMatrixVariableName} uniform not found on shader.");
            }

            // G = (M^-1)^T
            var modelMatrixWithoutTranslation = new Matrix4X4<float>(modelMatrix.Row1, modelMatrix.Row2, modelMatrix.Row3, modelMatrix.Row4);
            modelMatrixWithoutTranslation.M41 = 0;
            modelMatrixWithoutTranslation.M42 = 0;
            modelMatrixWithoutTranslation.M43 = 0;
            modelMatrixWithoutTranslation.M44 = 1;

            Matrix4X4<float> modelInvers;
            Matrix4X4.Invert<float>(modelMatrixWithoutTranslation, out modelInvers);
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInvers));

            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            CheckError();
        }

        private static unsafe void SetUniform1(string uniformName, float uniformValue)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.Uniform1(location, uniformValue);
            CheckError();
        }

        private static unsafe void SetUniform3(string uniformName, Vector3 uniformValue)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.Uniform3(location, uniformValue);
            CheckError();
        }

        private static unsafe void DrawModelObject(ModelObjectDescriptor modelObject)
        {
            Gl.BindVertexArray(modelObject.Vao);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, modelObject.Indices);
            Gl.DrawElements(PrimitiveType.Triangles, modelObject.IndexArrayLength, DrawElementsType.UnsignedInt, null);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
            Gl.BindVertexArray(0);
        }

        private static unsafe void SetMatrix(Matrix4X4<float> mx, string uniformName)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&mx);
            CheckError();
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("GL.GetError() returned " + error.ToString());
        }
    }
}