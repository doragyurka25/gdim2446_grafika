using System.Numerics;
using System.Runtime.CompilerServices;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace Szeminarium1_24_02_17_2
{
    internal static class Program
    {
        private static CameraDescriptor cameraDescriptor = new();

        private static CubeArrangementModel cubeArrangementModel = new();

        private static IWindow window;

        private static GL Gl;

        private static uint program;

        private static GlCube glCubeCentered;

        private static GlCube glCubeRotating;
        private static ImGuiController imGuiController;

        private static List<GlCube> cubies = new();
        private static List<Matrix4X4<float>> cubieRotation = new();

        private static System.Numerics.Vector3 lightColor = new(1.0f, 1.0f, 1.0f);
        private static System.Numerics.Vector3 lightPos = new(3.0f, 5.0f, 3.0f);

        private static float ambiens = 0.2f;
        private static float spekularis = 0.5f;
        private static float shininess = 50;

        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";
        private const string LayerRotationMatrixVariableName = "uLayerRotation";
        private const string LightColorVariableName = "uLightColor";
        private const string LightPositionVariableName = "uLightPos";
        private const string ViewPositionVariableName = "uViewPos";
        private const string AmbientStrengthVariableName = "uAmbientStrength";
        private const string SpecularStrengthVariableName = "uSpecularStrength";
        private const string ShinenessVariableName = "uShininess";


        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;
        layout (location = 2) in vec3 vNormal;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;
        uniform mat4 uLayerRotation;

		out vec4 outCol;
        out vec3 outNormal;
        out vec3 outWorldPosition;
        
        void main()
        {
            outCol = vCol;
            mat4 modelMat = uLayerRotation * uModel;
            vec4 worldPos = modelMat * vec4(vPos, 1.0);
            outWorldPosition = worldPos.xyz;
            
            outNormal = mat3(transpose(inverse(modelMat))) * vNormal;
            
            gl_Position = uProjection * uView * worldPos;
        }
        ";


        private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;

		in vec4 outCol;
        in vec3 outNormal;
        in vec3 outWorldPosition;

        uniform vec3 uLightColor;
        uniform vec3 uLightPos;
        uniform vec3 uViewPos;
        uniform float uAmbientStrength;
        uniform float uSpecularStrength;
        uniform float uShininess;

        void main()
        {
            vec3 objectColor = outCol.rgb;

            vec3 ambient = uAmbientStrength * uLightColor;

            vec3 norm = normalize(outNormal);
            vec3 lightDir = normalize(uLightPos - outWorldPosition);
            float diff = max(dot(norm, lightDir), 0.0);
            vec3 diffuse = diff * uLightColor;

            vec3 viewDir = normalize(uViewPos - outWorldPosition);
            vec3 reflectDir = reflect(-lightDir, norm);
            float spec = pow(max(dot(viewDir, reflectDir),0.0), uShininess);
            vec3 specular = uSpecularStrength * spec * uLightColor;

            vec3 result = (ambient + diffuse + specular) * objectColor;
            FragColor = vec4(result, outCol.a);
        }
        ";

        private static unsafe void SetLayerRotationMatrix(Matrix4X4<float> layerRotationMatrix)
        {
            int loc = Gl.GetUniformLocation(program, LayerRotationMatrixVariableName);

            if (loc == -1)
            {
                throw new Exception("Nincs a shaderbe a rotation matrix");
            }

            Gl.UniformMatrix4(loc, 1, false, (float*)&layerRotationMatrix);
            CheckError();
        }

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "2 szeminárium";
            windowOptions.Size = new Vector2D<int>(500, 500);

            // on some systems there is no depth buffer by default, so we need to make sure one is created
            windowOptions.PreferredDepthBufferBits = 24;

            window = Window.Create(windowOptions);

            window.Load += Window_Load;
            window.Update += Window_Update;
            window.Render += Window_Render;
            window.Closing += Window_Closing;

            window.Run();
        }

        private static void Window_Load()
        {
            //Console.WriteLine("Load");

            // set up input handling
            IInputContext inputContext = window.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            Gl = window.CreateOpenGL();
            Gl.ClearColor(System.Drawing.Color.White);

            LinkProgram();
            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();

            SetUpObjects();
            createRubiksCube();

            imGuiController = new ImGuiController(Gl, window, inputContext);

            //Gl.Enable(EnableCap.CullFace);

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
        }

        private static void LinkProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

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
            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }
            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            float step = 0.2f;
            if (ImGuiNET.ImGui.GetIO().WantCaptureKeyboard)
            {
                return;
            }
            switch (key)
            {
                case Key.Left:
                    cameraDescriptor.DecreaseZYAngle();
                    break;
                    ;
                case Key.Right:
                    cameraDescriptor.IncreaseZYAngle();
                    break;
                case Key.Down:
                    cameraDescriptor.MoveDown(step);
                    break;
                case Key.Up:
                    cameraDescriptor.MoveUp(step);
                    break;
                case Key.U:
                    cameraDescriptor.IncreaseZXAngle();
                    break;
                case Key.L:
                    cameraDescriptor.DecreaseZXAngle();
                    break;
                case Key.W:
                    cameraDescriptor.MoveForward(step); // Célpont felfelé mozgatása
                    break;
                case Key.S:
                    cameraDescriptor.MoveBack(step);
                    break;
                case Key.A:
                    cameraDescriptor.MoveLeft(step);
                    break;
                case Key.D:
                    cameraDescriptor.MoveRight(step);
                    break;

                case Key.Space:
                    cubeArrangementModel.rotateLayer(1.0f);
                    break;
                case Key.Backspace:
                    cubeArrangementModel.rotateLayer(-1.0f);
                    break;
            }
        }

        //Letrehozok 27 db kiskockat, a megfelelo szinekkel
        //mindegyiknek kulon Vao-ja van mert GlCube objectek
        private static void createRubiksCube()
        {
            float[] white = [1.0f, 1.0f, 1.0f, 1.0f];
            float[] red = [1.0f, 0.0f, 0.0f, 1.0f];
            float[] green = [0.0f, 1.0f, 0.0f, 1.0f];
            float[] blue = [0.0f, 0.0f, 1.0f, 1.0f];
            float[] black = [0.0f, 0.0f, 0.0f, 1.0f];
            float[] orange = [1.0f, 0.5f, 0.0f, 1.0f];
            float[] yellow = [1.0f, 1.0f, 0.0f, 1.0f];

            float[] felso, also, jobb, bal, elso, hatso;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        if (y == 1)
                        {
                            felso = white;
                        }
                        else felso = black;

                        if (y == -1)
                        {
                            also = orange;
                        }
                        else also = black;

                        if (x == -1)
                        {
                            bal = red;
                        }
                        else bal = black;

                        if (x == 1)
                        {
                            jobb = green;
                        }
                        else jobb = black;

                        if (z == 1)
                        {
                            elso = blue;
                        }
                        else elso = black;

                        if (z == -1)
                        {
                            hatso = yellow;
                        }
                        else hatso = black;

                        GlCube cubie = GlCube.CreateCubeWithFaceColors(Gl, felso, elso, bal, also, hatso, jobb);

                        cubies.Add(cubie);
                        cubieRotation.Add(Matrix4X4<float>.Identity);
                    }

                }
            }
        }

        private static void Window_Update(double deltaTime)
        {
            //Console.WriteLine($"Update after {deltaTime} [s].");
            // multithreaded
            // make sure it is threadsafe
            // NO GL calls
            //addig forgatjuk ami eleri a 90 fokos forgatast, ha vege akkor leallitjuk
            bool wasRotating = cubeArrangementModel.isLayerRotating;
            cubeArrangementModel.AdvanceTime(deltaTime);

            if (wasRotating && !cubeArrangementModel.isLayerRotating)
            {
                forgatas();
            }
        }

        private static void forgatas()
        {
            Matrix4X4<float> rotation = Matrix4X4.CreateRotationY(cubeArrangementModel.CurrentAngle);

            int index = 0;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {

                        if (y == 1)
                        {
                            cubieRotation[index] = cubieRotation[index] * rotation;
                        }
                        index++;
                    }
                }
            }
            cubeArrangementModel.ResetAngle();

        }

        private static unsafe void Window_Render(double deltaTime)
        {
            //Console.WriteLine($"Render after {deltaTime} [s].");
            imGuiController.Update((float)deltaTime);

            var io = ImGuiNET.ImGui.GetIO();
            io.DisplayFramebufferScale = new Vector2(
                (float)window.FramebufferSize.X / window.Size.X,
                (float)window.FramebufferSize.Y / window.Size.Y);

            Gl.Viewport(window.FramebufferSize);
            // GL here
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            Gl.UseProgram(program);

            SetUniform3(LightColorVariableName, lightColor);
            SetUniform3(LightPositionVariableName, lightPos);
            SetUniform3(ViewPositionVariableName, new Vector3(cameraDescriptor.Position.X, cameraDescriptor.Position.Y, cameraDescriptor.Position.Z));
            SetUniform1(AmbientStrengthVariableName, ambiens);
            SetUniform1(SpecularStrengthVariableName, spekularis);
            SetUniform1(ShinenessVariableName, shininess);

            ImGuiNET.ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 10), ImGuiNET.ImGuiCond.FirstUseEver);
            ImGuiNET.ImGui.SetNextWindowSize(new System.Numerics.Vector2(900, 620), ImGuiNET.ImGuiCond.FirstUseEver);

            ImGuiNET.ImGui.Begin("Feny es Navigacio", ImGuiNET.ImGuiWindowFlags.NoCollapse);

            ImGuiNET.ImGui.Text("Megvilagitas Szine (RGB)");
            ImGuiNET.ImGui.SliderFloat3("Feny Szin", ref lightColor, 0.0f, 1.0f);

            ImGuiNET.ImGui.Separator();
            ImGuiNET.ImGui.Text("Anyag es Feny Jellemzok");
            ImGuiNET.ImGui.SliderFloat("Ambient Erosség", ref ambiens, 0.0f, 1.0f);
            ImGuiNET.ImGui.SliderFloat("Specular Erosség", ref spekularis, 0.0f, 1.0f);
            ImGuiNET.ImGui.SliderFloat("Csillogas (Shininess)", ref shininess, 1.0f, 128.0f);

            ImGuiNET.ImGui.Separator();

            ImGuiNET.ImGui.Text("Fenyforras Pozicioja");
            ImGuiNET.ImGui.InputFloat3("Feny Poz", ref lightPos);

            ImGuiNET.ImGui.Separator();
            ImGuiNET.ImGui.Text("Forgatasok Navigacioja");

            if (ImGuiNET.ImGui.Button("Forgatas (+)"))
            {
                if (!cubeArrangementModel.isLayerRotating)
                    cubeArrangementModel.rotateLayer(1.0f);
            }
            if (ImGuiNET.ImGui.Button("Forgatas Ellenkezo (-)"))
            {
                if (!cubeArrangementModel.isLayerRotating)
                    cubeArrangementModel.rotateLayer(-1.0f);
            }
            ImGuiNET.ImGui.End();

            SetViewMatrix();
            SetProjectionMatrix();

            Matrix4X4<float> layerRotationMatrix = Matrix4X4<float>.Identity;
            if (cubeArrangementModel.isLayerRotating)
            {
                layerRotationMatrix = Matrix4X4.CreateRotationY(cubeArrangementModel.CurrentAngle);
            }

            float spacing = 1.0f;
            int index = 0;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        Matrix4X4<float> scale = Matrix4X4.CreateScale(0.9f);
                        Matrix4X4<float> translation = Matrix4X4.CreateTranslation(x * spacing, y * spacing, z * spacing);
                        Matrix4X4<float> modelMatrix = scale * translation * cubieRotation[index];
                        SetModelMatrix(modelMatrix);


                        if (y == 1)
                        {
                            SetLayerRotationMatrix(layerRotationMatrix);
                        }
                        else
                        {
                            SetLayerRotationMatrix(Matrix4X4<float>.Identity);
                        }

                        GlCube current = cubies[index];
                        Gl.BindVertexArray(current.Vao);
                        Gl.DrawElements(GLEnum.Triangles, current.IndexArrayLength, GLEnum.UnsignedInt, null);
                        Gl.BindVertexArray(0);

                        index++;
                    }
                }
            }

            imGuiController.Render();
        }

        private static unsafe void SetUniform3(string name, System.Numerics.Vector3 value)
        {
            int loc = Gl.GetUniformLocation(program, name);
            if (loc != -1)
            {
                Gl.Uniform3(loc, value);
            }
        }

        private static unsafe void SetUniform1(string name, float value)
        {
            int loc = Gl.GetUniformLocation(program, name);
            if (loc != -1)
            {
                Gl.Uniform1(loc, value);
            }
        }


        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {

            int location = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{ModelMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&modelMatrix);
            CheckError();
        }

        private static unsafe void SetUpObjects()
        {

            float[] face1Color = [0.0f, 0.0f, 0.0f, 1.0f];
            float[] face2Color = [0.0f, 1.0f, 0.0f, 1.0f];
            float[] face3Color = [0.0f, 0.0f, 0.0f, 1.0f];
            float[] face4Color = [1.0f, 0.0f, 0.0f, 1.0f];
            float[] face5Color = [0.0f, 0.0f, 1.0f, 1.0f];
            float[] face6Color = [0.0f, 0.0f, 0.0f, 1.0f];

            glCubeCentered = GlCube.CreateCubeWithFaceColors(Gl, face1Color, face2Color, face3Color, face4Color, face5Color, face6Color);

            face1Color = [0.5f, 0.0f, 0.0f, 1.0f];
            face2Color = [0.0f, 0.5f, 0.0f, 1.0f];
            face3Color = [0.0f, 0.0f, 0.5f, 1.0f];
            face4Color = [0.5f, 0.0f, 0.5f, 1.0f];
            face5Color = [0.0f, 0.5f, 0.5f, 1.0f];
            face6Color = [0.5f, 0.5f, 0.0f, 1.0f];

            glCubeRotating = GlCube.CreateCubeWithFaceColors(Gl, face1Color, face2Color, face3Color, face4Color, face5Color, face6Color);
        }



        private static void Window_Closing()
        {
            foreach (var cubie in cubies)
            {
                cubie.ReleaseGlCube();
            }
        }

        private static unsafe void SetProjectionMatrix()
        {
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)Math.PI / 4f, 1024f / 768f, 0.1f, 100);
            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);
            CheckError();
        }

        private static unsafe void SetViewMatrix()
        {
            var viewMatrix = Matrix4X4.CreateLookAt(cameraDescriptor.Position, cameraDescriptor.Target, cameraDescriptor.UpVector);
            int location = Gl.GetUniformLocation(program, ViewMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
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