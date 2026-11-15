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
        private static ModelObjectDescriptor diamondCube;
        private static CameraDescriptor camera = new CameraDescriptor();

        private static CubeArrangementModel cubeArrangementModel = new CubeArrangementModel();

        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private const string LightColorVariableName = "uLightColor";
        private const string LightPositionVariableName = "uLightPos";
        private const string ViewPositionVariableName = "uViewPos";

        private const string ShinenessVariableName = "uShininess";

        private static Vector3 ambiens = new Vector3(0.2f, 0.2f, 0.2f);
        private static Vector3 diffuz = new Vector3(0.8f, 0.8f, 0.8f);
        private static Vector3 spekularis = new Vector3(0.5f, 0.5f, 0.5f);

        private static Vector3 ambiensSzin = new Vector3(0.3f, 0.3f, 0.3f);
        private static Vector3 cubeFaceColor = new Vector3(1f, 0f, 0f);

        private const string AmbientStrengthVariableName = "uAmbientStrength";
        private const string DiffuseStrengthVariableName = "uDiffuseStrength";
        private const string SpecularStrengthVariableName = "uSpecularStrength";
        private const string AmbientColorVariableName = "uAmbientColor";
        private static float shininess = 50;

        private static uint program;

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Grafika szeminárium";
            windowOptions.Size = new Silk.NET.Maths.Vector2D<int>(500, 500);

            graphicWindow = Window.Create(windowOptions);

            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;
            graphicWindow.Closing += GraphicWindow_Closing;

            graphicWindow.Run();
        }

        private static void GraphicWindow_Closing()
        {
            cube.Dispose();
            imGuiController.Dispose();
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

            imGuiController = new ImGuiController(Gl, graphicWindow, inputContext);

            cube = ModelObjectDescriptor.CreateCube(Gl);

            updateCubeFaceColor();

            Gl.ClearColor(System.Drawing.Color.White);
            Gl.CullFace(TriangleFace.Back);

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);


            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, GetEmbeddedResourceAsString("Shaders.VertexShader.vert"));
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, GetEmbeddedResourceAsString("Shaders.FragmentShader.frag"));
            Gl.CompileShader(fshader);
            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
                throw new Exception("Fragment shader failed to compile: " + Gl.GetShaderInfoLog(fshader));

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);

            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
            if ((ErrorCode)Gl.GetError() != ErrorCode.NoError)
            {

            }

            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }
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
                case Key.A:
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

            //ez kellett ahhoz hogy az ablak skalazasa miatt meg mukodjon helyesen a rajzolas
            var io = ImGuiNET.ImGui.GetIO();
            io.DisplayFramebufferScale = new Vector2(
                (float)graphicWindow.FramebufferSize.X / graphicWindow.Size.X,
                (float)graphicWindow.FramebufferSize.Y / graphicWindow.Size.Y);

            //Az ImGui rendereles allitja a viewportot is, ha ezt nem allitom vissza
            //akkor osszenyomodik a rajz
            Gl.Viewport(graphicWindow.FramebufferSize);

            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            SetUniform3(AmbientStrengthVariableName, ambiens);
            SetUniform3(DiffuseStrengthVariableName, diffuz);
            SetUniform3(SpecularStrengthVariableName, spekularis);
            SetUniform3(AmbientColorVariableName, ambiensSzin);

            SetUniform3(LightColorVariableName, new Vector3(1f, 1f, 1f));
            SetUniform3(LightPositionVariableName, new Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z));
            SetUniform3(ViewPositionVariableName, new Vector3(camera.Position.X, camera.Position.Y, camera.Position.Z));
            SetUniform1(ShinenessVariableName, shininess);

            var viewMatrix = Matrix4X4.CreateLookAt(camera.Position, camera.Target, camera.UpVector);
            SetMatrix(viewMatrix, ViewMatrixVariableName);

            float aspect = (float)graphicWindow.FramebufferSize.X / graphicWindow.FramebufferSize.Y;
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)(Math.PI / 3), aspect, 0.1f, 100f);
            SetMatrix(projectionMatrix, ProjectionMatrixVariableName);



            var modelMatrixCenterCube = Matrix4X4.CreateScale((float)cubeArrangementModel.CenterCubeScale);
            SetModelMatrix(modelMatrixCenterCube);
            DrawModelObject(cube);

            //ez kellett ahhoz, hogy tudjam mozgatni a sslideres ablakot
            ImGuiNET.ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiNET.ImGuiCond.FirstUseEver);
            ImGuiNET.ImGui.SetNextWindowSize(new Vector2(380, 280), ImGuiNET.ImGuiCond.FirstUseEver);
            ImGuiNET.ImGui.Begin(
                "Anyagjellemzok es Feny",
                ImGuiNET.ImGuiWindowFlags.NoCollapse
            );

            ImGuiNET.ImGui.Text("Illuminacios komponensek (RGB)");
            ImGuiNET.ImGui.Separator();

            ImGuiNET.ImGui.SliderFloat3("Ambient Strength", ref ambiens, 0.0f, 2.0f);
            ImGuiNET.ImGui.SliderFloat3("Diffuse Strength", ref diffuz, 0.0f, 2.0f);
            ImGuiNET.ImGui.SliderFloat3("Specular Strength", ref spekularis, 0.0f, 2.0f);
            ImGuiNET.ImGui.SliderFloat("Shininess", ref shininess, 5f, 100f);

            ImGuiNET.ImGui.Separator();
            ImGuiNET.ImGui.Text("Hatterfeny (Ambient Light)");
            ImGuiNET.ImGui.SliderFloat3("Ambient Color", ref ambiensSzin, 0.0f, 1.0f);

            ImGuiNET.ImGui.Separator();
            ImGuiNET.ImGui.Text("Kocka szinezese");

            if (ImGuiNET.ImGui.ColorEdit3("Cube face color", ref cubeFaceColor))
            {
                updateCubeFaceColor();
            }

            ImGuiNET.ImGui.End();
            imGuiController.Render();
        }


        private static unsafe void updateCubeFaceColor()
        {
            //24 csucspont * 4 float(rgba)
            float[] colors = new float[24 * 4];

            //lapok szine
            //en a felso lap szinet akarom valtoztatni ezert az nem fix
            Vector3[] faceColors =
            {
                cubeFaceColor,
                new Vector3(0f, 1f, 0f),
                new Vector3(0f, 0f, 1f),
                new Vector3(1f, 0f, 1f),
                new Vector3(0f, 1f, 1f),
                new Vector3(1f, 1f, 0f)
            };

            //minden csucshoz ugyanazt a szin adja
            for (int f = 0; f < 6; f++)
            {
                for (int cs = 0; cs < 4; cs++)
                {
                    //hol kezdodik az adott csucs a tombben
                    int ind = (f * 4 + cs) * 4;
                    colors[ind + 0] = faceColors[f].X;
                    colors[ind + 1] = faceColors[f].Y;
                    colors[ind + 2] = faceColors[f].Z;
                    colors[ind + 3] = 1f;
                }
            }
            Gl.BindBuffer(GLEnum.ArrayBuffer, cube.Colors);

            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colors.AsSpan(), GLEnum.DynamicDraw);
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
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