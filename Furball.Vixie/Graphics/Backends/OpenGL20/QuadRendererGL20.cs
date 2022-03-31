using System;
using System.Drawing;
using System.Numerics;
using FontStashSharp;
using Furball.Vixie.FontStashSharp;
using Furball.Vixie.Graphics.Backends.OpenGL20.Abstractions;
using Furball.Vixie.Graphics.Backends.OpenGL41;
using Furball.Vixie.Graphics.Renderers;
using Furball.Vixie.Helpers;
using Silk.NET.OpenGL.Legacy;

namespace Furball.Vixie.Graphics.Backends.OpenGL20 {
    public class QuadRendererGL20 : IQuadRenderer {
        private readonly OpenGL20Backend _backend;
        private readonly GL              _gl;

        private readonly ProgramGL20 _program;
        private readonly uint        _vertexBuffer;

        private bool disposed = true;
        public void Dispose() {
            if (this.disposed) return;
            
            this._program.Dispose();
            this._gl.DeleteBuffer(this._vertexBuffer);

            this.disposed = true;
        }
        
        ~QuadRendererGL20() {
            DisposeQueue.Enqueue(this);
        }

        public bool IsBegun {
            get;
            set;
        }

        internal unsafe QuadRendererGL20(OpenGL20Backend backend) {
            this._backend = backend;
            this._gl      = backend.GetOpenGL();

            string vertex   = ResourceHelpers.GetStringResource("ShaderCode/OpenGL20/VertexShader.glsl");
            string fragment = QuadShaderGeneratorGL20.GetFragment(backend);

            this._program = new(this._backend, vertex, fragment);
            this._backend.CheckError();

            this._quadColorsUniformPosition             = this._program.GetUniformLocation("u_QuadColors");
            this._quadPositionsUniformPosition          = this._program.GetUniformLocation("u_QuadPositions");
            this._quadSizesUniformPosition              = this._program.GetUniformLocation("u_QuadSizes");
            this._quadRotationOriginsUniformPosition    = this._program.GetUniformLocation("u_QuadRotationOrigins");
            this._quadRotationsUniformPosition          = this._program.GetUniformLocation("u_QuadRotations");
            this._quadTextureIdsUniformPosition         = this._program.GetUniformLocation("u_QuadTextureIds");
            this._quadTextureCoordinatesUniformPosition = this._program.GetUniformLocation("u_QuadTextureCoordinates");
            this._backend.CheckError();

            for (int i = 0; i < backend.QueryMaxTextureUnits(); i++) {
                int location = this._program.GetUniformLocation($"tex_{i}");

                this._gl.Uniform1(location, i);
                this._backend.CheckError();
            }

            this._textRenderer = new VixieFontStashRenderer(this._backend, this);

            for (ushort i = 0; i < BATCH_COUNT; i++) {
                //Top left
                BatchedVertices[i * 4].VertexPosition.X          = 0;
                BatchedVertices[i * 4].VertexPosition.Y          = 0;
                BatchedVertices[i * 4].VertexTextureCoordinate.X = 0;
                BatchedVertices[i * 4].VertexTextureCoordinate.Y = 1;
                BatchedVertices[i * 4].VertexQuadIndex           = i;

                //Top right
                BatchedVertices[i * 4 + 1].VertexPosition.X          = 1;
                BatchedVertices[i * 4 + 1].VertexPosition.Y          = 0;
                BatchedVertices[i * 4 + 1].VertexTextureCoordinate.X = 1;
                BatchedVertices[i * 4 + 1].VertexTextureCoordinate.Y = 1;
                BatchedVertices[i * 4 + 1].VertexQuadIndex           = i;

                //Bottom left
                BatchedVertices[i * 4 + 2].VertexPosition.X          = 1;
                BatchedVertices[i * 4 + 2].VertexPosition.Y          = 1;
                BatchedVertices[i * 4 + 2].VertexTextureCoordinate.X = 1;
                BatchedVertices[i * 4 + 2].VertexTextureCoordinate.Y = 0;
                BatchedVertices[i * 4 + 2].VertexQuadIndex           = i;

                //Bottom right
                BatchedVertices[i * 4 + 3].VertexPosition.X          = 0;
                BatchedVertices[i * 4 + 3].VertexPosition.Y          = 1;
                BatchedVertices[i * 4 + 3].VertexTextureCoordinate.X = 0;
                BatchedVertices[i * 4 + 3].VertexTextureCoordinate.Y = 0;
                BatchedVertices[i * 4 + 3].VertexQuadIndex           = i;
            }

            for (ushort i = 0; i < BATCH_COUNT; i++) {
                //Top left
                BatchedIndicies[i * 6] = (ushort)(0 + i * 4);
                //Top right
                BatchedIndicies[i * 6 + 1] = (ushort)(1 + i * 4);
                //Bottom left
                BatchedIndicies[i * 6 + 2] = (ushort)(2 + i * 4);
                //Top right
                BatchedIndicies[i * 6 + 3] = (ushort)(2 + i * 4);
                //Bottom left
                BatchedIndicies[i * 6 + 4] = (ushort)(3 + i * 4);
                //Bottom right
                BatchedIndicies[i * 6 + 5] = (ushort)(0 + i * 4);
            }

            this._vertexBuffer = this._gl.GenBuffer();
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, this._vertexBuffer);
            this._backend.CheckError();
            _gl.BufferData<BatchedVertex>(BufferTargetARB.ArrayBuffer, BatchedVertices, BufferUsageARB.StaticDraw);
            this._backend.CheckError();

            _gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            this._backend.CheckError();
        }

        public unsafe void Begin() {
            this.IsBegun = true;

            this._program.Bind();

            fixed (void* ptr = &this._backend.ProjectionMatrix)
                this._gl.UniformMatrix4(this._program.GetUniformLocation("u_ProjectionMatrix"), 1, false, (float*)ptr);
            this._backend.CheckError();

            this._gl.BindBuffer(GLEnum.ArrayBuffer, this._vertexBuffer);

            this._gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)sizeof(BatchedVertex), (void*)0);
            this._gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, (uint)sizeof(BatchedVertex), (void*)sizeof(Vector2));
            this._gl.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, (uint)sizeof(BatchedVertex), (void*)(sizeof(Vector2) + sizeof(Vector2)));

            this._gl.EnableVertexAttribArray(0);
            this._gl.EnableVertexAttribArray(1);
            this._gl.EnableVertexAttribArray(2);

            this._gl.BindBuffer(GLEnum.ArrayBuffer, 0);
        }

        private int GetTextureId(Texture tex) {
            if (this.UsedTextures != 0)
                for (int i = 0; i < this.UsedTextures; i++) {
                    TextureGL20 tex2 = this.TextureArray[i];

                    if (tex2 == null) break;
                    if (tex  == tex2) return i;
                }

            this.TextureArray[this.UsedTextures] = (TextureGL20)tex;
            this.UsedTextures++;

            return this.UsedTextures - 1;
        }

        public void Draw(Texture textureGl, Vector2 position, Vector2 scale, float rotation, Color colorOverride, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
            if (!this.IsBegun)
                throw new Exception("Begin() has not been called!");

            //Ignore calls with invalid textures
            if (textureGl == null)
                return;

            if (scale.X == 0 || scale.Y == 0 || colorOverride.A == 0) return;

            if (this.BatchedQuads == BATCH_COUNT || this.UsedTextures == this._backend.QueryMaxTextureUnits()) {
                this.Flush();
                return;
            }

            this.BatchedColors[this.BatchedQuads]               = colorOverride;
            this.BatchedPositions[this.BatchedQuads]            = position;
            this.BatchedSizes[this.BatchedQuads]                = textureGl.Size * scale;
            this.BatchedRotationOrigins[this.BatchedQuads]      = rotOrigin;
            this.BatchedRotations[this.BatchedQuads]            = rotation;
            this.BatchedTextureIds[this.BatchedQuads]           = GetTextureId(textureGl);
            this.BatchedTextureCoordinates[this.BatchedQuads].X = 0;
            this.BatchedTextureCoordinates[this.BatchedQuads].Y = 0;
            this.BatchedTextureCoordinates[this.BatchedQuads].Z = texFlip == TextureFlip.FlipHorizontal ? -1 : 1;
            this.BatchedTextureCoordinates[this.BatchedQuads].W = texFlip == TextureFlip.FlipVertical ? -1 : 1;

            this.BatchedQuads++;
        }

        public void Draw(Texture textureGl, Vector2 position, Vector2 scale, float rotation, Color colorOverride, Rectangle sourceRect, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
            if (!this.IsBegun)
                throw new Exception("Begin() has not been called!");

            //Ignore calls with invalid textures
            if (textureGl == null || textureGl is not TextureGL20)
                return;

            if (scale.X == 0 || scale.Y == 0 || colorOverride.A == 0) return;

            if (this.BatchedQuads == BATCH_COUNT || this.UsedTextures == this._backend.QueryMaxTextureUnits()) {
                this.Flush();
                return;
            }

            //Set Size to the Source Rectangle
            Vector2 size = new Vector2(sourceRect.Width, sourceRect.Height);

            //Apply Scale
            size *= scale;

            sourceRect.Y = textureGl.Height - sourceRect.Y - sourceRect.Height;

            this.BatchedColors[this.BatchedQuads]               = colorOverride;
            this.BatchedPositions[this.BatchedQuads]            = position;
            this.BatchedSizes[this.BatchedQuads]                = size;
            this.BatchedRotationOrigins[this.BatchedQuads]      = rotOrigin;
            this.BatchedRotations[this.BatchedQuads]            = rotation;
            this.BatchedTextureIds[this.BatchedQuads]           = GetTextureId(textureGl);
            this.BatchedTextureCoordinates[this.BatchedQuads].X = (float)sourceRect.X      / textureGl.Width;
            this.BatchedTextureCoordinates[this.BatchedQuads].Y = (float)sourceRect.Y      / textureGl.Height;
            this.BatchedTextureCoordinates[this.BatchedQuads].Z = (float)sourceRect.Width  / textureGl.Width * (texFlip == TextureFlip.FlipHorizontal ? -1 : 1);
            this.BatchedTextureCoordinates[this.BatchedQuads].W = (float)sourceRect.Height / textureGl.Height * (texFlip == TextureFlip.FlipVertical ? -1 : 1);

            this.BatchedQuads++;
        }

        internal struct BatchedVertex {
            public Vector2 VertexPosition;
            public Vector2 VertexTextureCoordinate;
            public float   VertexQuadIndex;
        }

        private const int BATCH_COUNT = 128;
        
        private int _quadColorsUniformPosition;
        private int _quadPositionsUniformPosition;
        private int _quadSizesUniformPosition;
        private int _quadRotationOriginsUniformPosition;
        private int _quadRotationsUniformPosition;
        private int _quadTextureIdsUniformPosition;
        private int _quadTextureCoordinatesUniformPosition;

        private int BatchedQuads = 0;
        private int UsedTextures = 0;

        private BatchedVertex[] BatchedVertices           = new BatchedVertex[BATCH_COUNT * 4];
        private ushort[]        BatchedIndicies           = new ushort[BATCH_COUNT        * 6];
        private Color[]         BatchedColors             = new Color[BATCH_COUNT];
        private Vector2[]       BatchedPositions          = new Vector2[BATCH_COUNT];
        private Vector2[]       BatchedSizes              = new Vector2[BATCH_COUNT];
        private Vector2[]       BatchedRotationOrigins    = new Vector2[BATCH_COUNT];
        private float[]         BatchedRotations          = new float[BATCH_COUNT];
        private float[]         BatchedTextureIds         = new float[BATCH_COUNT];
        private Vector4[]       BatchedTextureCoordinates = new Vector4[BATCH_COUNT];

        private          TextureGL20[]          TextureArray = new TextureGL20[32];
        private readonly VixieFontStashRenderer _textRenderer;

        private unsafe void Flush() {
            if (this.BatchedQuads == 0) return;

            this._program.Bind();

            //Bind all the textures
            for (var i = 0; i < this.UsedTextures; i++) {
                TextureGL20 tex = this.TextureArray[i];

                if (tex == null) continue;

                this._gl.ActiveTexture(TextureUnit.Texture0 + i);
                this._gl.BindTexture(TextureTarget.Texture2D, tex.TextureId);
            }
            this._backend.CheckError();

            fixed (void* ptr = this.BatchedColors)
                this._gl.Uniform4(_quadColorsUniformPosition, (uint)this.BatchedQuads, (float*)ptr);
            this._backend.CheckError();

            fixed (void* ptr = this.BatchedPositions)
                this._gl.Uniform2(_quadPositionsUniformPosition, (uint)this.BatchedQuads, (float*)ptr);
            this._backend.CheckError();

            fixed (void* ptr = this.BatchedSizes)
                this._gl.Uniform2(_quadSizesUniformPosition, (uint)this.BatchedQuads, (float*)ptr);
            this._backend.CheckError();

            fixed (void* ptr = this.BatchedRotationOrigins)
                this._gl.Uniform2(_quadRotationOriginsUniformPosition, (uint)this.BatchedQuads, (float*)ptr);
            this._backend.CheckError();

            fixed (void* ptr = this.BatchedRotations)
                this._gl.Uniform1(_quadRotationsUniformPosition, (uint)this.BatchedQuads, (float*)ptr);
            this._backend.CheckError();

            fixed (void* ptr = this.BatchedTextureIds)
                this._gl.Uniform1(_quadTextureIdsUniformPosition, (uint)this.BatchedQuads, (float*)ptr);
            this._backend.CheckError();

            fixed (void* ptr = this.BatchedTextureCoordinates)
                this._gl.Uniform4(_quadTextureCoordinatesUniformPosition, (uint)this.BatchedQuads, (float*)ptr);
            this._backend.CheckError();

            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, this._vertexBuffer);

            this._gl.DrawElements<ushort>(PrimitiveType.Triangles, (uint)(this.BatchedQuads * 6), DrawElementsType.UnsignedShort, BatchedIndicies);

            this._backend.CheckError();


            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

            this.BatchedQuads = 0;
            this.UsedTextures = 0;
        }

        public void End() {
            this.IsBegun = false;
            this.Flush();

            this._gl.DisableVertexAttribArray(0);
            this._gl.DisableVertexAttribArray(1);
            this._gl.DisableVertexAttribArray(2);
        }

        #region overloads

        public void Draw(Texture textureGl, Vector2 position, float rotation = 0, TextureFlip flip = TextureFlip.None, Vector2 rotOrigin = default) {
            this.Draw(textureGl, position, Vector2.One, rotation, Color.White, flip, rotOrigin);
        }

        public void Draw(Texture textureGl, Vector2 position, Vector2 scale, float rotation = 0, TextureFlip flip = TextureFlip.None, Vector2 rotOrigin = default) {
            this.Draw(textureGl, position, scale, rotation, Color.White, flip, rotOrigin);
        }

        public void Draw(Texture textureGl, Vector2 position, Vector2 scale, Color colorOverride, float rotation = 0, TextureFlip texFlip = TextureFlip.None, Vector2 rotOrigin = default) {
            this.Draw(textureGl, position, scale, rotation, colorOverride, texFlip, rotOrigin);
        }

        #endregion

        #region text

        public void DrawString(DynamicSpriteFont font, string text, Vector2 position, Color color, float rotation = 0, Vector2? scale = null) {
            this.DrawString(font, text, position, color, rotation, scale, default);
        }

        /// <summary>
        /// Batches Text to the Screen
        /// </summary>
        /// <param name="font">Font to Use</param>
        /// <param name="text">Text to Write</param>
        /// <param name="position">Where to Draw</param>
        /// <param name="color">What color to draw</param>
        /// <param name="rotation">Rotation of the text</param>
        /// <param name="origin">The rotation origin of the text</param>
        /// <param name="scale">Scale of the text, leave null to draw at standard scale</param>
        public void DrawString(DynamicSpriteFont font, string text, Vector2 position, Color color, float rotation = 0f, Vector2? scale = null, Vector2 origin = default) {
            //Default Scale
            if (scale == null || scale == Vector2.Zero)
                scale = Vector2.One;

            //Draw
            font.DrawText(this._textRenderer, text, position, System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B), scale.Value, rotation, origin);
        }
        /// <summary>
        /// Batches Text to the Screen
        /// </summary>
        /// <param name="font">Font to Use</param>
        /// <param name="text">Text to Write</param>
        /// <param name="position">Where to Draw</param>
        /// <param name="color">What color to draw</param>
        /// <param name="rotation">Rotation of the text</param>
        /// <param name="scale">Scale of the text, leave null to draw at standard scale</param>
        public void DrawString(DynamicSpriteFont font, string text, Vector2 position, System.Drawing.Color color, float rotation = 0f, Vector2? scale = null) {
            //Default Scale
            if (scale == null || scale == Vector2.Zero)
                scale = Vector2.One;

            //Draw
            font.DrawText(this._textRenderer, text, position, color, scale.Value, rotation);
        }
        /// <summary>
        /// Batches Colorful text to the Screen
        /// </summary>
        /// <param name="font">Font to Use</param>
        /// <param name="text">Text to Write</param>
        /// <param name="position">Where to Draw</param>
        /// <param name="colors">What colors to use</param>
        /// <param name="rotation">Rotation of the text</param>
        /// <param name="scale">Scale of the text, leave null to draw at standard scale</param>
        public void DrawString(DynamicSpriteFont font, string text, Vector2 position, System.Drawing.Color[] colors, float rotation = 0f, Vector2? scale = null) {
            //Default Scale
            if (scale == null || scale == Vector2.Zero)
                scale = Vector2.One;

            //Draw
            font.DrawText(this._textRenderer, text, position, colors, scale.Value, rotation);
        }

        #endregion
    }
}