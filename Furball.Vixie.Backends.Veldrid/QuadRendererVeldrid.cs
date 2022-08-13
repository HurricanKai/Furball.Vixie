using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Backends.Veldrid.Abstractions;
using Furball.Vixie.Helpers;
using Furball.Vixie.Helpers.Helpers;
using Veldrid;
using Veldrid.SPIRV;
using Rectangle=System.Drawing.Rectangle;

namespace Furball.Vixie.Backends.Veldrid; 

internal class QuadRendererVeldrid : IQuadRenderer {
    private readonly VeldridBackend _backend;
        
    public bool IsBegun {
        get;
        set;
    }

    private Pipeline _pipeline;
        
    [StructLayout(LayoutKind.Sequential)]
    private struct InstanceData {
        public Vector2 InstancePosition;
        public Vector2 InstanceSize;
        public Color   InstanceColor;
        public Vector2 InstanceTextureRectPosition;
        public Vector2 InstanceTextureRectSize;
        public Vector2 InstanceRotationOrigin;
        public float   InstanceRotation;
        public int     InstanceTextureId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Vertex {
        public Vector2 VertexPosition;
        public Vector2 TextureCoordinate;
    }

    private readonly DeviceBuffer _projectionBuffer;
    private readonly ResourceSet  _projectionBufferResourceSet;
        
    private readonly DeviceBuffer _instanceVertexBuffer;
    private readonly DeviceBuffer _vertexBuffer; //TODO: maybe this can be static?
    private readonly DeviceBuffer _indexBuffer;  //TODO: maybe this can be static?

    private static ushort[] _Indicies = new ushort[] {
        //Tri 1
        0, 1, 2,
        //Tri 2
        2, 3, 0
    };

    private static Vertex[] _Vertices = new Vertex[] {
        //Bottom left
        new() {
            VertexPosition    = new(0, 1),
            TextureCoordinate = new(0, 0)
        },
        //Bottom right
        new() {
            VertexPosition    = new(1, 1),
            TextureCoordinate = new(1, 0)
        },
        //Top right
        new() {
            VertexPosition    = new(1, 0),
            TextureCoordinate = new(1, 1)
        },
        //Top left
        new() {
            VertexPosition    = new(0, 0),
            TextureCoordinate = new(0, 1)
        }
    };
        
    public unsafe QuadRendererVeldrid(VeldridBackend backend) {
        this._backend = backend;
        this._backend.CheckThread();

        string vertexSource   = ResourceHelpers.GetStringResource("Shaders/QuadRenderer/VertexShader.glsl");
        string fragmentSource = ResourceHelpers.GetStringResource("Shaders/QuadRenderer/FragmentShader.glsl");

        ShaderDescription vertexShaderDescription   = new ShaderDescription(ShaderStages.Vertex,   Encoding.UTF8.GetBytes(vertexSource),   "main");
        ShaderDescription fragmentShaderDescription = new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragmentSource), "main");

        Shader[] shaders = this._backend.ResourceFactory.CreateFromSpirv(vertexShaderDescription, fragmentShaderDescription);

        VertexLayoutDescription vtxLayout = new VertexLayoutDescription(new[] {
            new VertexElementDescription("VertexPosition",    VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate), 
            new VertexElementDescription("TextureCoordinate", VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate)
        }) {
            InstanceStepRate = 0
        };

        VertexLayoutDescription instanceVtxLayout = new VertexLayoutDescription(new[] {
            new VertexElementDescription("InstancePosition",            VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate), 
            new VertexElementDescription("InstanceSize",                VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate),
            new VertexElementDescription("InstanceColor",               VertexElementFormat.Float4, VertexElementSemantic.TextureCoordinate),
            new VertexElementDescription("InstanceTextureRectPosition", VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate),
            new VertexElementDescription("InstanceTextureRectSize",     VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate),
            new VertexElementDescription("InstanceRotationOrigin",      VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate),
            new VertexElementDescription("InstanceRotation",            VertexElementFormat.Float1, VertexElementSemantic.TextureCoordinate),
            new VertexElementDescription("InstanceTextureId",           VertexElementFormat.Int1,   VertexElementSemantic.TextureCoordinate),
        }) {
            InstanceStepRate = 1
        };

        #region create projection buffer
        BufferDescription projBufDesc = new BufferDescription((uint)sizeof(Matrix4x4), BufferUsage.UniformBuffer);
        this._projectionBuffer = this._backend.ResourceFactory.CreateBuffer(projBufDesc);
            
        ResourceSetDescription projBufResourceSetDesc = new() {
            BoundResources = new[] {
                this._projectionBuffer
            },
            Layout = this._backend.ResourceFactory.CreateResourceLayout(new(new[] {
                new ResourceLayoutElementDescription("ProjectionMatrixUniform", ResourceKind.UniformBuffer, ShaderStages.Vertex)
            }))
        };
        this._projectionBufferResourceSet = this._backend.ResourceFactory.CreateResourceSet(projBufResourceSetDesc);
        #endregion

        GraphicsPipelineDescription pipelineDescription = new() {
            ShaderSet = new ShaderSetDescription {
                Shaders = shaders,
                VertexLayouts = new[] {
                    vtxLayout, instanceVtxLayout
                }
            },
            Outputs           = backend.RenderFramebuffer.OutputDescription,
            BlendState        = BlendStateDescription.SingleAlphaBlend,
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            ResourceLayouts = new[] {
                projBufResourceSetDesc.Layout,
                VixieTextureVeldrid.ResourceLayouts[0],
                VixieTextureVeldrid.ResourceLayouts[1],
                VixieTextureVeldrid.ResourceLayouts[2],
                VixieTextureVeldrid.ResourceLayouts[3]
            },
            RasterizerState = new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, true)
        };

        this._pipeline = backend.ResourceFactory.CreateGraphicsPipeline(pipelineDescription);

        this._boundTextures = new VixieTextureVeldrid[backend.QueryMaxTextureUnits()];

        #region Create render buffers
        BufferDescription vtxBufferDesc         = new BufferDescription((uint)sizeof(Vertex)       * 4,             BufferUsage.VertexBuffer);
        BufferDescription instanceVtxBufferDesc = new BufferDescription((uint)sizeof(InstanceData) * NUM_INSTANCES, BufferUsage.VertexBuffer);

        BufferDescription indexBufferDesc = new BufferDescription((uint)sizeof(ushort) * 6, BufferUsage.IndexBuffer);
            
        this._vertexBuffer         = this._backend.ResourceFactory.CreateBuffer(vtxBufferDesc);
        this._instanceVertexBuffer = this._backend.ResourceFactory.CreateBuffer(instanceVtxBufferDesc);

        this._indexBuffer = this._backend.ResourceFactory.CreateBuffer(indexBufferDesc);
            
        //Fill our vertex and index buffer
        this._backend.GraphicsDevice.UpdateBuffer(this._vertexBuffer, 0, _Vertices);
        this._backend.GraphicsDevice.UpdateBuffer(this._indexBuffer,  0, _Indicies);
        #endregion
    }
        
    public void Begin() {
        this._backend.CheckThread();
        this.IsBegun = true;
            
        this._backend.CommandList.SetPipeline(this._pipeline);

        //Update the UBO with the projection matrix
        this._backend.CommandList.UpdateBuffer(this._projectionBuffer, 0, this._backend.ProjectionMatrix);
        this._backend.CommandList.SetGraphicsResourceSet(0, this._projectionBufferResourceSet);
            
        //Set the index buffer
        this._backend.CommandList.SetIndexBuffer(this._indexBuffer, IndexFormat.UInt16);
        //Set the main vertex buffer
        this._backend.CommandList.SetVertexBuffer(0, this._vertexBuffer);
        //Set the vertex buffer that contains our instance data
        this._backend.CommandList.SetVertexBuffer(1, this._instanceVertexBuffer);

        for (uint i = 0; i < VeldridBackend.MAX_TEXTURE_UNITS; i++) {
            this._backend.CommandList.SetGraphicsResourceSet(i + 1, this._backend.WhitePixelResourceSet);
        }
    }

    public void Draw(VixieTexture vixieTexture, Vector2 position, Vector2 scale, float rotation, Color colorOverride, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
        this._backend.CheckThread();
        if (!this.IsBegun)
            throw new Exception("Begin() has not been called!");

        //Ignore calls with invalid textures
        if (vixieTexture == null || vixieTexture is not VixieTextureVeldrid textureVeldrid)
            return;

        if (this._instances >= NUM_INSTANCES || this._usedTextures == this._backend.QueryMaxTextureUnits()) {
            this.Flush();
        }
            
        this._instanceData[this._instances].InstancePosition              = position;
        this._instanceData[this._instances].InstanceSize                  = new Vector2(vixieTexture.Size.X, vixieTexture.Size.Y) * scale;
        this._instanceData[this._instances].InstanceColor                 = colorOverride;
        this._instanceData[this._instances].InstanceRotation              = rotation;
        this._instanceData[this._instances].InstanceRotationOrigin        = rotOrigin;
        this._instanceData[this._instances].InstanceTextureId             = this.GetTextureId(textureVeldrid);
        this._instanceData[this._instances].InstanceTextureRectPosition.X = 0;
        this._instanceData[this._instances].InstanceTextureRectPosition.Y = 0;
        this._instanceData[this._instances].InstanceTextureRectSize.X     = texFlip == TextureFlip.FlipHorizontal ? -1 : 1;
        this._instanceData[this._instances].InstanceTextureRectSize.Y     = texFlip == TextureFlip.FlipVertical ? -1 : 1;

        if(textureVeldrid.IsFbAndShouldFlip)
            this._instanceData[this._instances].InstanceTextureRectSize.Y *= -1;
            
        this._instances++;
    }
        
    private int GetTextureId(VixieTextureVeldrid tex) {
        this._backend.CheckThread();
        if(tex.UsedId != -1) return tex.UsedId;
            
        if(this._usedTextures != 0)
            for (int i = 0; i < this._usedTextures; i++) {
                VixieTexture tex2 = this._boundTextures[i];

                if (tex2 == null) break;
                if (tex  == tex2) return i;
            }

        this._boundTextures[this._usedTextures] = tex;

        tex.UsedId = this._usedTextures;
            
        this._usedTextures++;

        return this._usedTextures - 1;
    }

    private          uint             _instances    = 0;
    private readonly InstanceData[]   _instanceData = new InstanceData[NUM_INSTANCES];
    private readonly VixieTextureVeldrid[] _boundTextures;
    private          int              _usedTextures = 0;

    public void Draw(VixieTexture vixieTexture, Vector2 position, Vector2 scale, float rotation, Color colorOverride, Rectangle sourceRect, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
        this._backend.CheckThread();
        if (!this.IsBegun)
            throw new Exception("Begin() has not been called!");

        //Ignore calls with invalid textures
        if (vixieTexture == null || vixieTexture is not VixieTextureVeldrid textureVeldrid)
            return;

        if (this._instances >= NUM_INSTANCES || this._usedTextures == this._backend.QueryMaxTextureUnits()) {
            this.Flush();
        }

        //Set Size to the Source Rectangle
        Vector2 size = new Vector2(sourceRect.Width, sourceRect.Height);

        //Apply Scale
        size *= scale;
            
        sourceRect.Y = vixieTexture.Height - sourceRect.Y - sourceRect.Height;

        this._instanceData[this._instances].InstancePosition              = position;
        this._instanceData[this._instances].InstanceSize                  = size;
        this._instanceData[this._instances].InstanceColor                 = colorOverride;
        this._instanceData[this._instances].InstanceRotation              = rotation;
        this._instanceData[this._instances].InstanceRotationOrigin        = rotOrigin;
        this._instanceData[this._instances].InstanceTextureId             = this.GetTextureId(textureVeldrid);
        this._instanceData[this._instances].InstanceTextureRectPosition.X = (float)sourceRect.X                       / vixieTexture.Width;
        this._instanceData[this._instances].InstanceTextureRectPosition.Y = (float)sourceRect.Y                       / vixieTexture.Height;
        this._instanceData[this._instances].InstanceTextureRectSize.X     = (float)sourceRect.Width  / vixieTexture.Width  * (texFlip == TextureFlip.FlipHorizontal ? -1 : 1);
        this._instanceData[this._instances].InstanceTextureRectSize.Y     = (float)sourceRect.Height / vixieTexture.Height * (texFlip == TextureFlip.FlipVertical ? -1 : 1);

        if(textureVeldrid.IsFbAndShouldFlip)
            this._instanceData[this._instances].InstanceTextureRectSize.Y *= -1;
            
        this._instances++;
    }
        
    private const int NUM_INSTANCES = 1024;

    private unsafe void Flush() {
        this._backend.CheckThread();
        if (this._instances == 0) return;

        //Iterate through all used textures and bind them
        for (int i = 0; i < this._usedTextures; i++) {
            //Bind the texture to the resource sets
            this._backend.CommandList.SetGraphicsResourceSet((uint)(i + 1), this._boundTextures[i].GetResourceSet(this._backend, i));

            this._boundTextures[i].UsedId = -1;
        }

        //Update the vertex buffer with just the data we use
        fixed (void* ptr = this._instanceData)
            this._backend.CommandList.UpdateBuffer(this._instanceVertexBuffer, 0, (IntPtr)ptr, (uint)(sizeof(InstanceData) * this._instances));

        //Draw the data to the screen
        this._backend.CommandList.DrawIndexed(6, this._instances, 0, 0, 0);
            
        this._instances    = 0;
        this._usedTextures = 0;
    }

    public void End() {
        this._backend.CheckThread();
        this.Flush();
        this.IsBegun = false;
    }

    private bool _isDisposed = false;
    public void Dispose() {
        this._backend.CheckThread();
        if (this._isDisposed) return;
        this._isDisposed = true;

        this._pipeline.Dispose();
        this._indexBuffer.Dispose();
        this._projectionBuffer.Dispose();
        this._vertexBuffer.Dispose();
        this._instanceVertexBuffer.Dispose();
        this._projectionBufferResourceSet.Dispose();
    }

    ~QuadRendererVeldrid() {
        DisposeQueue.Enqueue(this);
    }
        
    public void Draw(VixieTexture vixieTexture, Vector2 position, float rotation = 0, TextureFlip flip = TextureFlip.None, Vector2 rotOrigin = default) {
        this.Draw(vixieTexture, position, Vector2.One, rotation, Color.White, flip, rotOrigin);
    }

    public void Draw(VixieTexture vixieTexture, Vector2 position, Vector2 scale, float rotation = 0, TextureFlip flip = TextureFlip.None, Vector2 rotOrigin = default) {
        this.Draw(vixieTexture, position, scale, rotation, Color.White, flip, rotOrigin);
    }

    public void Draw(VixieTexture vixieTexture, Vector2 position, Vector2 scale, Color colorOverride, float rotation = 0, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
        this.Draw(vixieTexture, position, scale, rotation, colorOverride, texFlip, rotOrigin);
    }
}