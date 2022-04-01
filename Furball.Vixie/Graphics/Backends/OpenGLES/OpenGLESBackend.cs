using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Threading;
using Furball.Vixie.Graphics.Backends.OpenGL_;
using Furball.Vixie.Graphics.Backends.OpenGL41;
using Furball.Vixie.Graphics.Backends.OpenGLES.Abstractions;
using Furball.Vixie.Graphics.Renderers;
using Furball.Vixie.Helpers;
using Kettu;
using Silk.NET.Core.Native;
using Silk.NET.OpenGLES;
using Silk.NET.OpenGLES.Extensions.ImGui;
using Silk.NET.Windowing;
using BufferTargetARB=Silk.NET.OpenGL.BufferTargetARB;
using BufferUsageARB=Silk.NET.OpenGL.BufferUsageARB;

namespace Furball.Vixie.Graphics.Backends.OpenGLES {
    // ReSharper disable once InconsistentNaming
    public class OpenGLESBackend : GraphicsBackend, IGLBasedBackend {
        /// <summary>
        /// OpenGLES API
        /// </summary>
        // ReSharper disable once InconsistentNaming
        private GL gl;
        /// <summary>
        /// Projection Matrix used to go from Window Coordinates to OpenGLES Coordinates
        /// </summary>
        internal Matrix4x4 ProjectionMatrix;
        /// <summary>
        /// Cache for the Maximum amount of Texture units allowed by the device
        /// </summary>
        private int _maxTextureUnits = -1;
        /// <summary>
        /// ImGui Controller
        /// </summary>
        internal ImGuiController ImGuiController;
        /// <summary>
        /// Stores the Main Thread that OpenGLES commands run on, used to ensure that OpenGLES commands don't run on different threads
        /// </summary>
        private static Thread _mainThread;
        /// <summary>
        /// Gets the Thread of Operation
        /// </summary>
        [Conditional("DEBUG")]
        private void GetMainThread() {
            _mainThread = Thread.CurrentThread;
        }
        /// <summary>
        /// Ensures that OpenGLES commands don't run on the wrong thread
        /// </summary>
        /// <exception cref="ThreadStateException">Throws if a cross-thread operation has occured</exception>
        [Conditional("DEBUG")]
        internal void CheckThread() {
            if (Thread.CurrentThread != _mainThread)
                throw new ThreadStateException("You are calling GL on the wrong thread!");
        }
        /// <summary>
        /// Used to Initialize the Backend
        /// </summary>
        /// <param name="window"></param>
        public override void Initialize(IWindow window) {
            this.GetMainThread();

            this.gl = window.CreateOpenGLES();

#if DEBUGWITHGL
            unsafe {
                //Enables Debugging
                gl.Enable(EnableCap.DebugOutput);
                gl.Enable(EnableCap.DebugOutputSynchronous);
                gl.DebugMessageCallback(this.Callback, null);
            }
#endif

            //Enables Blending (Required for Transparent Objects)
            this.gl.Enable(EnableCap.Blend);
            this.gl.BlendFunc(GLEnum.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            this.ImGuiController = new ImGuiController(gl, Global.GameInstance.WindowManager.GameWindow, Global.GameInstance._inputContext);
            
            Logger.Log($"OpenGL Version: {this.gl.GetStringS(StringName.Version)}",                LoggerLevelOpenGLES.InstanceInfo);
            Logger.Log($"GLSL Version:   {this.gl.GetStringS(StringName.ShadingLanguageVersion)}", LoggerLevelOpenGLES.InstanceInfo);
            Logger.Log($"OpenGL Vendor:  {this.gl.GetStringS(StringName.Vendor)}",                 LoggerLevelOpenGLES.InstanceInfo);
            Logger.Log($"Renderer:       {this.gl.GetStringS(StringName.Renderer)}",               LoggerLevelOpenGLES.InstanceInfo);
        }
        public void CheckError(string message = "") {
            this.CheckErrorInternal();
        }
        /// <summary>
        /// Checks for OpenGL errors
        /// </summary>
        [Conditional("DEBUG")]
        public void CheckErrorInternal(string message = "") {
            GLEnum error = this.gl.GetError();
            
            if (error != GLEnum.NoError) {
#if DEBUGWITHGL
                throw new Exception($"Got GL Error {error}!");
#else
                Debugger.Break();
                Logger.Log($"OpenGLES Error! Code: {error.ToString()}", LoggerLevelOpenGLES.InstanceError);
#endif
            }
        }
        /// <summary>
        /// Used to Cleanup the Backend
        /// </summary>
        public override void Cleanup() {
            this.gl.Dispose();
        }
        /// <summary>
        /// Used to Handle the Window size Changing
        /// </summary>
        /// <param name="width">New width</param>
        /// <param name="height">New height</param>
        public override void HandleWindowSizeChange(int width, int height) {
            this.gl.Viewport(0, 0, (uint) width, (uint) height);

            this.ProjectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, width, 0, height, 1f, 0f);
        }
        /// <summary>
        /// Used to handle the Framebuffer Resizing
        /// </summary>
        /// <param name="width">New width</param>
        /// <param name="height">New height</param>
        public override void HandleFramebufferResize(int width, int height) {
            this.gl.Viewport(0, 0, (uint) width, (uint) height);
        }
        /// <summary>
        /// Used to Create a Texture Renderer
        /// </summary>
        /// <returns>A Texture Renderer</returns>
        public override IQuadRenderer CreateTextureRenderer() {
            return new QuadRendererGLES(this);
        }
        /// <summary>
        /// Used to Create a Line Renderer
        /// </summary>
        /// <returns></returns>
        public override ILineRenderer CreateLineRenderer() {
            return new LineRendererGLES(this);
        }
        /// <summary>
        /// Gets the Amount of Texture Units available for use
        /// </summary>
        /// <returns>Amount of Texture Units supported</returns>
        public override int QueryMaxTextureUnits() {
            if (this._maxTextureUnits == -1) {
                this.gl.GetInteger(GetPName.MaxTextureImageUnits, out int maxTexSlots);
                this._maxTextureUnits = maxTexSlots;
            }

            return this._maxTextureUnits;
        }
        /// <summary>
        /// Clears the Screen
        /// </summary>
        public override void Clear() {
            this.gl.Clear(ClearBufferMask.ColorBufferBit);
        }
        /// <summary>
        /// Used to Create a TextureRenderTarget
        /// </summary>
        /// <param name="width">Width of the Target</param>
        /// <param name="height">Height of the Target</param>
        /// <returns></returns>
        public override TextureRenderTarget CreateRenderTarget(uint width, uint height) {
            return new TextureRenderTargetGLES(this, width, height);
        }
        /// <summary>
        /// Creates a Texture given some Data
        /// </summary>
        /// <param name="imageData">Image Data</param>
        /// <param name="qoi">Is the Data in the QOI format?</param>
        /// <returns>Texture</returns>
        public override Texture CreateTexture(byte[] imageData, bool qoi = false) {
            return new TextureGLES(this, imageData, qoi);
        }
        /// <summary>
        /// Creates a Texture given a Stream
        /// </summary>
        /// <param name="stream">Stream to read from</param>
        /// <returns>Texture</returns>
        public override Texture CreateTexture(Stream stream) {
            return new TextureGLES(this, stream);
        }
        /// <summary>
        /// Creates a Empty Texture given a Size
        /// </summary>
        /// <param name="width">Width of Texture</param>
        /// <param name="height">Height of Texture</param>
        /// <returns>Texture</returns>
        public override Texture CreateTexture(uint width, uint height) {
            return new TextureGLES(this, width, height);
        }
        /// <summary>
        /// Creates a Texture from a File
        /// </summary>
        /// <param name="filepath">Filepath to Image</param>
        /// <returns>Texture</returns>
        public override Texture CreateTexture(string filepath) {
            return new TextureGLES(this, filepath);
        }
        /// <summary>
        /// Used to Create a 1x1 Texture with only a white pixel
        /// </summary>
        /// <returns>White Pixel Texture</returns>
        public override Texture CreateWhitePixelTexture() {
            return new TextureGLES(this);
        }
        /// <summary>
        /// Used to Update the ImGuiController in charge of rendering ImGui on this backend
        /// </summary>
        /// <param name="deltaTime">Delta Time</param>
        public override void ImGuiUpdate(double deltaTime) {
            this.ImGuiController.Update((float)deltaTime);
        }
        /// <summary>
        /// Used to Draw the ImGuiController in charge of rendering ImGui on this backend
        /// </summary>
        /// <param name="deltaTime">Delta Time</param>
        public override void ImGuiDraw(double deltaTime) {
            this.ImGuiController.Render();
        }
        /// <summary>
        /// Returns the OpenGLES API
        /// </summary>
        /// <returns></returns>
        public GL GetGlApi() => this.gl;
        /// <summary>
        /// Debug Callback
        /// </summary>
        private void Callback(GLEnum source, GLEnum type, int id, GLEnum severity, int length, nint message, nint userparam) {
            string stringMessage = SilkMarshal.PtrToString(message);

            LoggerLevel level = severity switch {
                GLEnum.DebugSeverityHigh         => LoggerLevelDebugMessageCallback.InstanceHigh,
                GLEnum.DebugSeverityMedium       => LoggerLevelDebugMessageCallback.InstanceMedium,
                GLEnum.DebugSeverityLow          => LoggerLevelDebugMessageCallback.InstanceLow,
                GLEnum.DebugSeverityNotification => LoggerLevelDebugMessageCallback.InstanceNotification,
                _                                => null
            };

            Console.WriteLine(stringMessage);
        }
        public GLBackendType             GetType()     => GLBackendType.ES;
        public Silk.NET.OpenGL.GL        GetModernGL() => throw new WrongGLBackendException();
        public Silk.NET.OpenGL.Legacy.GL GetLegacyGL() => throw new WrongGLBackendException();
        public GL                        GetGLES()     => this.gl;
        
        public uint                      GenBuffer()   => this.gl.GenBuffer();
        public void BindBuffer(BufferTargetARB usage, uint buf) {
            this.gl.BindBuffer((Silk.NET.OpenGLES.BufferTargetARB)usage, buf);
        }
        public unsafe void BufferData(BufferTargetARB bufferType, nuint size, void* data, BufferUsageARB bufferUsage) {
            this.gl.BufferData((Silk.NET.OpenGLES.BufferTargetARB)bufferType, size, data, (Silk.NET.OpenGLES.BufferUsageARB)bufferUsage);
        }
        public unsafe void BufferSubData(BufferTargetARB bufferType, nint offset, nuint size, void* data) {
            this.gl.BufferSubData((Silk.NET.OpenGLES.BufferTargetARB)bufferType, offset, size, data);
        }
        public void DeleteBuffer(uint bufferId) {
            this.gl.DeleteBuffer(bufferId);
        }
    }
}
