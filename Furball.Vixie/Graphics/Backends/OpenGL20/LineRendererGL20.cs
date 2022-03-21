using System;
using System.Numerics;
using Furball.Vixie.Graphics.Backends.OpenGL20.Abstractions;
using Furball.Vixie.Graphics.Renderers;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGL.Legacy;

namespace Furball.Vixie.Graphics.Backends.OpenGL20 {
    public class LineRendererGL20 : ILineRenderer {
        private struct LineData {
            public Vector2 Position;
            public Color   Color;
        }
        
        private readonly OpenGL20Backend _backend;

        private const int BATCH_MAX = 128; //cut this in two for actual line could, as we use 2 LineData structs per line :^)
        
        private readonly ProgramGL20 _program;
        private readonly uint        _arrayBuf;

        private          int        _batchedLines = 0;
        private readonly LineData[] _lineData     = new LineData[BATCH_MAX];
        
        internal unsafe LineRendererGL20(OpenGL20Backend backend) {
            this._backend = backend;
            this._gl      = backend.GetOpenGL();
            
            string vertex   = ResourceHelpers.GetStringResource("ShaderCode/OpenGL20/LineVertexShader.glsl");
            string fragment = ResourceHelpers.GetStringResource("ShaderCode/OpenGL20/LineFragmentShader.glsl");

            this._program = new ProgramGL20(backend, vertex, fragment);

            this._arrayBuf = this._gl.GenBuffer();
            this._gl.BindBuffer(BufferTargetARB.ArrayBuffer, this._arrayBuf);
            //Fill the buffer with empty
            this._gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(sizeof(LineData) * BATCH_MAX), null, BufferUsageARB.DynamicDraw);
            
            this._gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        }

        public void Dispose() {
            
        }
        ~LineRendererGL20() {
            this._program.Dispose();
        }
        public bool IsBegun {
            get;
            set;
        }
        public unsafe void Begin() {
            this.IsBegun = true;
            
            this._program.Bind();

            fixed (void* ptr = &this._backend.ProjectionMatrix)
                this._gl.UniformMatrix4(this._program.GetUniformLocation("u_ProjectionMatrix"), 1, false, (float*)ptr);
            this._backend.CheckError();
            
            this._gl.BindBuffer(GLEnum.ArrayBuffer, this._arrayBuf);
            
            this._gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)sizeof(LineData), (void*)0);
            this._gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, (uint)sizeof(LineData), (void*)sizeof(Vector2));
            
            this._gl.EnableVertexAttribArray(0);
            this._gl.EnableVertexAttribArray(1);
            
            this._gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        }

        private          float       lastThickness = 0;
        private readonly GL          _gl;
        public void Draw(Vector2 begin, Vector2 end, float thickness, Color color) {
            if (!this.IsBegun) throw new Exception("LineRenderer is not begun!!");

            if (thickness == 0 || color.A == 0) return;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (thickness != this.lastThickness || this._batchedLines == BATCH_MAX) {
                this.Flush();
                this.lastThickness = thickness;
            }

            this._lineData[this._batchedLines].Position     = begin;
            this._lineData[this._batchedLines + 1].Position = end;
            this._lineData[this._batchedLines].Color        = color;
            this._lineData[this._batchedLines + 1].Color    = color;

            this._batchedLines += 2;
        }

        private void Flush() {
            if (this._batchedLines == 0 || this.lastThickness == 0) return;

            this._program.Bind();
            
            this._gl.LineWidth(this.lastThickness);

            this._gl.BindBuffer(GLEnum.ArrayBuffer, this._arrayBuf);
            
            this._gl.BufferSubData<LineData>(GLEnum.ArrayBuffer, 0, this._lineData);

            this._gl.DrawArrays(PrimitiveType.Lines, 0, (uint)this._batchedLines);

            this._gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            this._program.Unbind();

            this._batchedLines = 0;
            this.lastThickness = 0;
        }
        
        public void End() {
            this.IsBegun = false;
            Flush();

            this._gl.DisableVertexAttribArray(0);
            this._gl.DisableVertexAttribArray(1);
        }
    }
}
