using NAudio.Dsp;
using NAudio.Wave;
using OpenTK.Compute.OpenCL;
using OpenTK.Core;

using OpenTK.Mathematics;
using System.Numerics;
using System.Text;
using Complex = NAudio.Dsp.Complex;
using SysVector3 = System.Numerics.Vector3;
using GLVector3 = OpenTK.Mathematics.Vector3;
using System.Reflection;
using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL4;
//using OpenTK.Graphics.ES20;


namespace WinForms_TestApp
{
    public enum AnalysisType
    {
        Frequency,
        Waveform,
        Sonogram,
        Sonogram3D
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct Sonogram3DVertex
    {
        float x, y, z;
        float u, v;
    }

    public partial class MainForm : Form
    {
        private static DebugProc DebugMessageDelegate = OnDebugMessage;

        string testFile = "birds-ambiance-204513-mono.mp3";
        string testPath = "C:\\Users\\Zsolt\\source\\repos\\WinForms_TestApp\\";
        int frames = 0;
        int fftSize = 2048;
        bool canceling = false;
        public int FrequencyBinCount => fftSize / 2;

        //Visualizer suff
        AnalysisType analysisType = AnalysisType.Sonogram3D;

        const int TEXTURE_HEIGHT = 256;
        const int SONOGRAM_3D_WIDTH = 256;
        const int SONOGRAM_3D_HEIGHT = 256;
        const float SONOGRAM_3D_GEOMETRY_SIZE = 9.5f;
        const int ATTRIBUTE_POSITION_INDEX = 0;
        const int ATTRIBUTE_TEXTURE_COORDINATE_INDEX = 1;
        int VBO_BufferID = 0;
        int EBO_BufferID = 0;
        int VAO_BufferID = 0;
        int sonogram3DNumIndices = 0;
        int FrequencyDataTextureID = 0;
        byte[] freqByteData = [];
        int yoffset = 0;

        Shader Sonogram3DShader = null;

        // Background color
        Color4 backgroundColor = new(0.08f, 0.08f, 0.08f, 1);
        Color4 foregroundColor = new(0, 0.7f, 0, 1);

        //camera
        public float Camera_xRot = -180;
        public float Camera_yRot = 270;
        public float Camera_zRot = 90;
        public float Camera_xT = 0;
        public float Camera_yT = -2;
        public float Camera_zT = -2;
        public float Camera_scaleFactor = 3.0f;
        public bool Camera_dragging = false;
        public float Camera_curX = 0;
        public float Camera_curY = 0;
        private void InitGL()
        {
            GlControl_MainView.MakeCurrent();
            GL.Viewport(GlControl_MainView.Location.X, GlControl_MainView.Location.Y, GlControl_MainView.Width, GlControl_MainView.Height);
            GL.ClearColor(backgroundColor);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Texture2D);

            GL.DebugMessageCallback(DebugMessageDelegate, IntPtr.Zero);
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);

            int numVertices = SONOGRAM_3D_WIDTH * SONOGRAM_3D_HEIGHT;
            if (numVertices > ushort.MaxValue + 1)
            { 
                throw new Exception("Sonogram 3D resolution is too high: can only handle 65536 vertices max");
            }

            //Here the vertecies are defined for the 3D Sonogram, the layout will have the (x,y,z,u,v) values in that order
            //The x,y,z values are self explanitory positions, the u,v are texture coordinates
            float[] vertices = new float[numVertices * 5];

            for (int z = 0; z < SONOGRAM_3D_HEIGHT; z++)
            {
                for (int x = 0; x < SONOGRAM_3D_WIDTH; x++)
                {
                    // Generate a reasonably fine mesh in the X-Z plane
                    vertices[5 * (SONOGRAM_3D_WIDTH * z + x) + 0] = SONOGRAM_3D_GEOMETRY_SIZE * (x - SONOGRAM_3D_WIDTH / 2) / SONOGRAM_3D_WIDTH;//x
                    vertices[5 * (SONOGRAM_3D_WIDTH * z + x) + 1] = 0;//y
                    vertices[5 * (SONOGRAM_3D_WIDTH * z + x) + 2] = SONOGRAM_3D_GEOMETRY_SIZE * (z - SONOGRAM_3D_HEIGHT / 2) / SONOGRAM_3D_HEIGHT;//z
                    vertices[5 * (SONOGRAM_3D_WIDTH * z + x) + 3] = x / (float)(SONOGRAM_3D_WIDTH - 1);//u
                    vertices[5 * (SONOGRAM_3D_WIDTH * z + x) + 4] = z / (float)(SONOGRAM_3D_HEIGHT - 1);//v
                }
            }

            VAO_BufferID = GL.GenVertexArray();
            GL.BindVertexArray(VAO_BufferID);

            VBO_BufferID = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO_BufferID);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertices.Length * sizeof(float)), vertices, BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(ATTRIBUTE_POSITION_INDEX);
            GL.VertexAttribPointer(ATTRIBUTE_POSITION_INDEX, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(ATTRIBUTE_TEXTURE_COORDINATE_INDEX);
            GL.VertexAttribPointer(ATTRIBUTE_TEXTURE_COORDINATE_INDEX, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

            int localSonogram3DNumIndices = (SONOGRAM_3D_WIDTH - 1) * (SONOGRAM_3D_HEIGHT - 1) * 6;
            sonogram3DNumIndices = localSonogram3DNumIndices - (6 * 600);

            ushort[] indices = new ushort[localSonogram3DNumIndices];
            //comment from the original code
            // We need to use TRIANGLES instead of for example TRIANGLE_STRIP
            // because we want to make one draw call instead of hundreds per
            // frame, and unless we produce degenerate triangles (which are very
            // ugly) we won't be able to split the rows.
            int idx = 0;
            for (int z = 0; z < SONOGRAM_3D_HEIGHT - 1; z++)
            {
                for (int x = 0; x < SONOGRAM_3D_WIDTH - 1; x++)
                {
                    indices[idx++] = (ushort)(z * SONOGRAM_3D_WIDTH + x);
                    indices[idx++] = (ushort)(z * SONOGRAM_3D_WIDTH + x + 1);
                    indices[idx++] = (ushort)((z + 1) * SONOGRAM_3D_WIDTH + x + 1);
                    indices[idx++] = (ushort)(z * SONOGRAM_3D_WIDTH + x);
                    indices[idx++] = (ushort)((z + 1) * SONOGRAM_3D_WIDTH + x + 1);
                    indices[idx++] = (ushort)((z + 1) * SONOGRAM_3D_WIDTH + x);
                }
            }

            EBO_BufferID = GL.GenBuffer();

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO_BufferID);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(indices.Length * sizeof(ushort)), indices, BufferUsageHint.StaticDraw);

            //implement shaders

            string vertexPath = testPath + "Shaders\\sonogram-vertex.shader";
            string fragmentPath = testPath + "Shaders\\sonogram-fragment.shader";
            if (!File.Exists(vertexPath) || !File.Exists(fragmentPath))
            {
                throw new Exception("Sonogram vertex or fragment shader files missing!");
            }

            string vertexCode = File.ReadAllText(vertexPath);
            string fragmentCode = File.ReadAllText(fragmentPath);

            Sonogram3DShader = new Shader(vertexCode, fragmentCode);

            if (Sonogram3DShader.AttributeLocations["gPosition"] != ATTRIBUTE_POSITION_INDEX ||
                Sonogram3DShader.AttributeLocations["gTexCoord0"] != ATTRIBUTE_TEXTURE_COORDINATE_INDEX)
            {
                throw new Exception("The sonogram vertex shader has missaligned attributes!");
            }

            //Initialise the frequency data texture
            freqByteData = new byte[FrequencyBinCount];

            // (Re-)Allocate the texture object
            if (FrequencyDataTextureID != 0)
            {
                GL.DeleteTexture(FrequencyDataTextureID);
            }
            FrequencyDataTextureID = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, FrequencyDataTextureID);

            GL.TextureParameter(FrequencyDataTextureID, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TextureParameter(FrequencyDataTextureID, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TextureParameter(FrequencyDataTextureID, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TextureParameter(FrequencyDataTextureID, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            byte[] tmp = new byte[freqByteData.Length * TEXTURE_HEIGHT];
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R8, freqByteData.Length, TEXTURE_HEIGHT, 0, PixelFormat.Red, PixelType.UnsignedByte, tmp);

            //InitTriangle();

            //GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            //GL.BindVertexArray(0);
            //GL.GetError();

        }
        private void DrawGL()
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, FrequencyDataTextureID);
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, yoffset, freqByteData.Length, 1, PixelFormat.Red, PixelType.UnsignedByte, freqByteData);

            //The current texture is taken from the currently active texture unit, which in this case is 0
            //this constant is only here for readability
            const int currentTexture = 0;

            yoffset = (yoffset + 1) % TEXTURE_HEIGHT;

            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO_BufferID);
            //test
            //GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw);

            GL.BindVertexArray(VAO_BufferID);
            Sonogram3DShader.UseShader();

            GL.Uniform1(Sonogram3DShader.UniformLocations["vertexFrequencyData"], currentTexture);
            float normalizedYOffset = (float)yoffset / (TEXTURE_HEIGHT - 1);

            GL.Uniform1(Sonogram3DShader.UniformLocations["yoffset"], normalizedYOffset);
            float discretizedYOffset = MathF.Floor(normalizedYOffset * (SONOGRAM_3D_HEIGHT - 1)) / (SONOGRAM_3D_HEIGHT - 1);

            GL.Uniform1(Sonogram3DShader.UniformLocations["vertexYOffset"], discretizedYOffset);
            GL.Uniform1(Sonogram3DShader.UniformLocations["verticalScale"], SONOGRAM_3D_GEOMETRY_SIZE / 3.5f);

            // Set up the model, view and projection matrices
            Matrix4x4 projection = Matrix4x4.Identity;
            Matrix4x4 view = Matrix4x4.Identity;
            Matrix4x4 model = Matrix4x4.Identity;
            Matrix4x4 mvp = Matrix4x4.Identity;

            projection = projection.Perspective(55, (float)GlControl_MainView.Width / GlControl_MainView.Height, 1, 100);
            view = view.Translate(0.0f, 0.0f, -9.0f);
            model = model.Rotate(Camera_xRot, 1, 0, 0);
            model = model.Rotate(Camera_yRot, 0, 1, 0);
            model = model.Rotate(Camera_zRot, 0, 0, 1);
            model = model.Translate(Camera_xT, Camera_yT, Camera_zT);

            // Compute necessary matrices
            mvp *= model;
            mvp *= view;
            mvp *= projection;

            Matrix4 mvpGL = mvp.ToMatrix4();

            GL.UniformMatrix4(Sonogram3DShader.UniformLocations["worldViewProjection"], false, ref mvpGL);

            int frequencyDataLoc = Sonogram3DShader.UniformLocations["frequencyData"];
            int foregroundColorLoc = Sonogram3DShader.UniformLocations["foregroundColor"];
            int backgroundColorLoc = Sonogram3DShader.UniformLocations["backgroundColor"];

            if (frequencyDataLoc >= 0)
            {
                GL.Uniform1(frequencyDataLoc, currentTexture);
            }
            if (foregroundColorLoc >= 0)
            {
                GL.Uniform4(foregroundColorLoc, foregroundColor);
            }
            if (backgroundColorLoc >= 0)
            {
                GL.Uniform4(backgroundColorLoc, backgroundColor);
            }

            // Clear the render area
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Actually draw
            // Note: this expects the element array buffer to still be bound
            GL.DrawElements(PrimitiveType.Triangles, sonogram3DNumIndices, DrawElementsType.UnsignedShort, 0);


            // Disable the attribute arrays for cleanliness
            //GL.DisableVertexAttribArray(positionIndex);
            //GL.DisableVertexAttribArray(textureCoordinatesIndex);

            frames++;
            Label_Frames.Text = frames.ToString();
            GlControl_MainView.SwapBuffers();
        }

        CancellationTokenSource cts = new();
        Task bck = null;
        public MainForm()
        {
            InitializeComponent();
        }
        private void AudioTest(CancellationToken cts)
        {
            using var audioFile = new AudioFileReader(testPath + testFile);

            // Create a custom WaveProvider that will analyze audio while playing
            var sampleProvider = audioFile.ToSampleProvider();
            var bufferedWaveProvider = new BufferedWaveProvider(audioFile.WaveFormat);
            var analyzer = new AudioAnalyzer(sampleProvider, bufferedWaveProvider);
            using var waveOut = new WaveOutEvent();
            waveOut.Init(analyzer);
            waveOut.Volume = 0.1f;
            waveOut.Play();

            while (waveOut.PlaybackState == PlaybackState.Playing && !canceling)
            {
                freqByteData = analyzer.GetByteFrequencyData();
                //for (int i = 0; i > freqByteData.Length; i++)
                //{
                //    freqByteData[i] = 120;
                //}

                Invoke(() =>
                {
                    //StringBuilder sb = new();
                    //foreach (float fl in freqByteData)
                    //{
                    //    sb.Append(',' + fl.ToString());
                    //}
                    //TextBox_Data.Text = sb.ToString();
                    DrawGL();
                });

                Thread.Sleep(10);  // Adjust for refresh rate
            }
            waveOut.Stop();
        }
        //
        private void GlControl_MainView_Paint(object sender, PaintEventArgs e)
        {
            //DrawGL();
        }

        private void GlControl_MainView_Load(object sender, EventArgs e)
        {
            Console.WriteLine("Init Console Done.");
            InitGL();
            bck = new Task(() => AudioTest(cts.Token));
            bck.Start();
        }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            cts.Cancel();
            canceling = true;
            //bck.Wait();
        }

        private static void OnDebugMessage(
            DebugSource source,     // Source of the debugging message.
            DebugType type,         // Type of the debugging message.
            int id,                 // ID associated with the message.
            DebugSeverity severity, // Severity of the message.
            int length,             // Length of the string in pMessage.
            IntPtr pMessage,        // Pointer to message string.
            IntPtr pUserParam)      // The pointer you gave to OpenGL, explained later.
        {
            // In order to access the string pointed to by pMessage, you can use Marshal
            // class to copy its contents to a C# string without unsafe code. You can
            // also use the new function Marshal.PtrToStringUTF8 since .NET Core 1.1.
            string message = Marshal.PtrToStringUTF8(pMessage, length);

            // The rest of the function is up to you to implement, however a debug output
            // is always useful.
            Console.WriteLine("[{0} source={1} type={2} id={3}] {4}", severity, source, type, id, message);

            // Potentially, you may want to throw from the function for certain severity
            // messages.
            if (type == DebugType.DebugTypeError)
            {
                throw new Exception(message);
            }
        }
    }
}
