using Silk.NET.OpenGL;

namespace Furball.Vixie.Backends.OpenGL.Abstractions; 

public enum GLBackendType {
    Modern,
    ES,
    Legacy
}

internal interface IGLBasedBackend {
    public float VerticalRatio { get; set; }
        
    public GL                        GetModernGL();
    public Silk.NET.OpenGL.Legacy.GL GetLegacyGL();
    public Silk.NET.OpenGLES.GL      GetGLES();

    public uint GenBuffer();

    public void BindBuffer(BufferTargetARB usage, uint buf);

    public unsafe void BufferData(BufferTargetARB bufferType, nuint size, void* data, BufferUsageARB bufferUsage);

    public unsafe void BufferSubData(BufferTargetARB bufferType, nint offset, nuint size, void* data);

    public void DeleteBuffer(uint bufferId);

    public void DeleteFramebuffer(uint frameBufferId);

    public void DeleteTexture(uint textureId);

    public void DeleteRenderbuffer(uint bufId);

    public void DrawBuffers(uint i, in GLEnum[] drawBuffers);

    public void BindFramebuffer(FramebufferTarget framebuffer, uint frameBufferId);

    public uint GenFramebuffer();

    public void BindTexture(TextureTarget target, uint textureId);

    public void BindTextures(uint[] textures, uint count);
        
    public unsafe void TexImage2D(TextureTarget target, int level, InternalFormat format, uint width, uint height, int border, PixelFormat pxFormat, PixelType type, void* data);

    public void TexParameterI(TextureTarget target, GLEnum param, int paramData);

    public uint GenRenderbuffer();

    public void Viewport(int x, int y, uint width, uint height);

    public uint GenTexture();

    public void BindRenderbuffer(RenderbufferTarget target, uint id);

    public void RenderbufferStorage(RenderbufferTarget target, InternalFormat format, uint width, uint height);

    public void FramebufferRenderbuffer(FramebufferTarget target, FramebufferAttachment attachment, RenderbufferTarget rbTarget, uint id);

    public void FramebufferTexture(FramebufferTarget target, FramebufferAttachment colorAttachment0, uint textureId, int level);

    public GLEnum CheckFramebufferStatus(FramebufferTarget target);

    public void GetInteger(GetPName viewport, ref int[] oldViewPort);

    public void TexParameter(TextureTarget target, TextureParameterName paramName, int param);

    public unsafe void TexSubImage2D(TextureTarget target, int level, int x, int y, uint width, uint height, PixelFormat pxformat, PixelType pxtype, void* data);

    public void ActiveTexture(TextureUnit textureSlot);

    public uint CreateProgram();

    public uint CreateShader(ShaderType type);

    public void ShaderSource(uint shaderId, string source);
    public void GetShader(uint shaderId, ShaderParameterName paramName, out int returnValue);

    public void CompileShader(uint shaderId);

    public string GetShaderInfoLog(uint shaderId);

    public void AttachShader(uint programId, uint shaderId);

    public void LinkProgram(uint programId);

    public void GetProgram(uint programId, ProgramPropertyARB linkStatus, out int i);

    public void DeleteShader(uint shader);

    public string GetProgramInfoLog(uint programId);

    public void UseProgram(uint programId);

    public int GetUniformLocation(uint programId, string uniformName);

    public unsafe void UniformMatrix4(int getUniformLocation, uint i, bool b, float* f);

    public void Uniform1(int getUniformLocation, float f);

    public void Uniform1(int getUniformLocation, int f);

    public void Uniform2(int getUniformLocation, float f, float f2);

    public        void Uniform2(int getUniformLocation, int  f,     int    f2);
    public unsafe void Uniform4(int getUniformLocation, uint count, float* ptr);

    public void DeleteProgram(uint programId);

    public uint GenVertexArray();

    public void EnableVertexAttribArray(uint u);

    public unsafe void VertexAttribPointer(uint u, int currentElementCount, VertexAttribPointerType currentElementType, bool currentElementNormalized, uint getStride, void* offset);

    public unsafe void VertexAttribIPointer(uint u, int currentElementCount, VertexAttribIType vertexAttribIType, uint getStride, void* offset);

    public void BindVertexArray(uint arrayId);

    public void DeleteVertexArray(uint arrayId);

    public void   CheckError(string error);
    public void   GlCheckThread();
    unsafe void   Uniform2(int              getUniformLocation, uint count, float* ptr);
    unsafe void   Uniform1(int              getUniformLocation, uint count, float* ptr);
    void          VertexAttribDivisor(uint  iOffset,            uint currentElementInstanceDivisor);
    internal void GenerateMipmaps(VixieTextureGL vixieTextureGl);
    unsafe   void GetTexImage(TextureTarget target, int level, PixelFormat format, PixelType type, void* ptr);
    void SetProjectionMatrixAndViewport(int targetWidth, int targetHeight, bool flip);
}