using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using FontStashSharp;
using Furball.Vixie.Graphics.Renderers;
using Furball.Vixie.Helpers;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace Furball.Vixie.Graphics.Backends.Direct3D11 {
    public unsafe class QuadRendererD3D11 : IQuadRenderer {
        public bool IsBegun { get; set; }

        private Direct3D11Backend _backend;
        private DeviceContext _deviceContext;

        private Buffer       _vertexBuffer;
        private InputLayout  _inputLayout;
        private VertexShader _vertexShader;
        private PixelShader  _pixelShader;

        [StructLayout(LayoutKind.Sequential)]
        struct VertexData {
            public Vector2 Position;
            public Vector2 Scale;
            public float   Rotation;
            public Vector4 Color;
            public Vector2 RotationOrigin;
        }

        private VertexData[] _localVertexBuffer;
        private VertexData*  _vertexBufferPointer;
        private int          _currentVertex;

        public unsafe QuadRendererD3D11(Direct3D11Backend backend) {
            this._backend       = backend;
            this._deviceContext = backend.GetDeviceContext();

            string shaderSourceCode = ResourceHelpers.GetStringResource("ShaderCode/Direct3D11/QuadRenderer/Shaders.hlsl", true);

            CompilationResult vertexShaderResult = ShaderBytecode.Compile(shaderSourceCode, "VS_Main", "vs_5_0", ShaderFlags.EnableStrictness, EffectFlags.None, System.Array.Empty<ShaderMacro>(), null, "VertexShader.hlsl");
            CompilationResult pixelShaderResult = ShaderBytecode.Compile(shaderSourceCode, "PS_Main", "ps_5_0", ShaderFlags.EnableStrictness, EffectFlags.None, System.Array.Empty<ShaderMacro>(), null, "PixelShader.hlsl");

            InputElement[] elementDescription = new [] {
                new InputElement("POSITION",  0, Format.R32G32_Float,       (int) Marshal.OffsetOf<VertexData>("Position"),       0),
                new InputElement("SCALE",     0, Format.R32G32_Float,       (int) Marshal.OffsetOf<VertexData>("Scale"),          0),
                new InputElement("ROTATION",  0, Format.R32_Float,          (int) Marshal.OffsetOf<VertexData>("Rotation"),       0),
                new InputElement("COLOR",     0, Format.R32G32B32A32_Float, (int) Marshal.OffsetOf<VertexData>("Color"),          0),
                new InputElement("ROTORIGIN", 0, Format.R32G32_Float,       (int) Marshal.OffsetOf<VertexData>("RotationOrigin"), 0),
            };

            InputLayout layout = new InputLayout(backend.GetDevice(), vertexShaderResult.Bytecode.Data, elementDescription);
            this._inputLayout = layout;

            int vertexBufferSize = sizeof(VertexData) * 512;

            BufferDescription description = new BufferDescription {
                BindFlags   = BindFlags.VertexBuffer,
                SizeInBytes = vertexBufferSize,
                Usage       = ResourceUsage.Default
            };

            Buffer vertexBuffer = new Buffer(backend.GetDevice(), description);
            this._vertexBuffer = vertexBuffer;

            this._vertexShader = new VertexShader(backend.GetDevice(), vertexShaderResult.Bytecode.Data);
            this._pixelShader  = new PixelShader(backend.GetDevice(), pixelShaderResult.Bytecode.Data);
        }

        public void Begin() {
            this.IsBegun            = true;
            this._localVertexBuffer = new VertexData[512];

            fixed (VertexData* ptr = this._localVertexBuffer)
                this._vertexBufferPointer = ptr;
        }
        public void Draw(Texture textureGl, Vector2 position, Vector2 scale, float rotation, Color colorOverride, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
            this._vertexBufferPointer->Position = new Vector2(0, 0);
            this._vertexBufferPointer->Color    = new Vector4(1.0f, 0, 0, 1);
            this._vertexBufferPointer++;

            this._vertexBufferPointer->Position = new Vector2(1, 0);
            this._vertexBufferPointer->Color    = new Vector4(0, 1.0f, 0, 1);
            this._vertexBufferPointer++;

            this._vertexBufferPointer->Position = new Vector2(0.5f, 1);
            this._vertexBufferPointer->Color    = new Vector4(0, 0, 1.0f, 1);
            this._vertexBufferPointer++;

            this._deviceContext.UpdateSubresource(this._localVertexBuffer, this._vertexBuffer);

            this._deviceContext.InputAssembler.InputLayout       = this._inputLayout;
            this._deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            this._deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(this._vertexBuffer, sizeof(VertexData), 0));
            this._deviceContext.VertexShader.Set(this._vertexShader);
            this._deviceContext.PixelShader.Set(this._pixelShader);
            this._backend.SetRenderTargets();

            this._deviceContext.Draw(3, 0);
        }
        public void Draw(Texture textureGl, Vector2 position, Vector2 scale, float rotation, Color colorOverride, Rectangle sourceRect, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {

        }

        public void Draw(Texture textureGl, Vector2 position, float rotation = 0, TextureFlip flip = TextureFlip.None, Vector2 rotOrigin = default) {
            this.Draw(textureGl, position, Vector2.One, rotation, Color.White, flip, rotOrigin);
        }

        public void Draw(Texture textureGl, Vector2 position, Vector2 scale, float rotation = 0, TextureFlip flip = TextureFlip.None, Vector2 rotOrigin = default) {
            this.Draw(textureGl, position, scale, rotation, Color.White, flip, rotOrigin);
        }

        public void Draw(Texture textureGl, Vector2 position, Vector2 scale, Color colorOverride, float rotation = 0, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
            this.Draw(textureGl, position, scale, rotation, colorOverride, texFlip, rotOrigin);
        }

        public void DrawString(DynamicSpriteFont font, string text, Vector2 position, Color color, float rotation = 0, Vector2? scale = null) {
            throw new System.NotImplementedException();
        }

        public void DrawString(DynamicSpriteFont font, string text, Vector2 position, System.Drawing.Color color, float rotation = 0, Vector2? scale = null) {
            throw new System.NotImplementedException();
        }

        public void DrawString(DynamicSpriteFont font, string text, Vector2 position, System.Drawing.Color[] colors, float rotation = 0, Vector2? scale = null) {
            throw new System.NotImplementedException();
        }

        public void End() {

        }

        public void Dispose() {
            throw new System.NotImplementedException();
        }
    }
}
