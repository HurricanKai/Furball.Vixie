﻿using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using FontStashSharp;
using FontStashSharp.Interfaces;
using Furball.Vixie.Backends.Shared;
using Furball.Vixie.Backends.Shared.Renderers;
using Furball.Vixie.Helpers;
using Color = Furball.Vixie.Backends.Shared.Color;

namespace Furball.Vixie;

public static class RendererExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector2 FlipVector2(Vector2 vec, TextureFlip flip) => new(
        flip.HasFlag(TextureFlip.FlipHorizontal) ? -vec.X : vec.X,
        flip.HasFlag(TextureFlip.FlipVertical) ? -vec.Y : vec.Y);

    private static unsafe void SetQuadVertices(Renderer renderer, Vertex* ptr, Vector2 pos, Vector2 size, VixieTexture tex, Color color, TextureFlip flip) {
        long texId = renderer.GetTextureId(tex);
        ptr[0] = new Vertex {
            Position          = pos,
            Color             = color,
            TextureCoordinate = FlipVector2(new Vector2(0, tex.InternalFlip ? 1 : 0), flip),
            TexId             = texId
        };
        ptr[1] = new Vertex {
            Position = pos + size with {
                Y = 0
            },
            Color             = color,
            TextureCoordinate = FlipVector2(new Vector2(1, tex.InternalFlip ? 1 : 0), flip),
            TexId             = texId
        };
        ptr[2] = new Vertex {
            Position          = pos + size,
            Color             = color,
            TextureCoordinate = FlipVector2(new Vector2(1, tex.InternalFlip ? 0 : 1), flip),
            TexId             = texId
        };
        ptr[3] = new Vertex {
            Position = pos + size with {
                X = 0
            },
            Color             = color,
            TextureCoordinate = FlipVector2(new Vector2(0, tex.InternalFlip ? 0 : 1), flip),
            TexId             = texId
        };
    }

    private static unsafe void SetQuadIndices(MappedData mappedData) {
        //Tri 1
        mappedData.IndexPtr[0] = (ushort)(3 + mappedData.IndexOffset);
        mappedData.IndexPtr[1] = (ushort)(2 + mappedData.IndexOffset);
        mappedData.IndexPtr[2] = (ushort)(0 + mappedData.IndexOffset);
        //Tri 2
        mappedData.IndexPtr[3] = (ushort)(2 + mappedData.IndexOffset);
        mappedData.IndexPtr[4] = (ushort)(1 + mappedData.IndexOffset);
        mappedData.IndexPtr[5] = (ushort)(0 + mappedData.IndexOffset);
    }

    public static unsafe void AllocateUnrotatedTexturedQuad(this Renderer renderer, VixieTexture tex, Vector2 position,
                                                            Vector2        scale,    Color        color, TextureFlip flip = TextureFlip.None) {
        Vector2 size = new(tex.Width * scale.X, tex.Height * scale.Y);

        MappedData mappedData = renderer.Reserve(4, 6);

        SetQuadVertices(renderer, mappedData.VertexPtr, position, size, tex, color, flip);
        SetQuadIndices(mappedData);
    }

    public static unsafe void AllocateUnrotatedTexturedQuadWithSourceRect(this Renderer renderer, VixieTexture tex,
                                                                          Vector2        position,
                                                                          Vector2        scale, Rectangle sourceRect,
                                                                          Color          color, TextureFlip flip = TextureFlip.None) {
        Vector2 size = new(sourceRect.Width * scale.X, sourceRect.Height * scale.Y);

        if (tex.InternalFlip)
            sourceRect.Height *= -1;
        
        MappedData mappedData = renderer.Reserve(4, 6);
        SetQuadVertices(renderer, mappedData.VertexPtr, position, size, tex, color, flip);
        mappedData.VertexPtr[0].TextureCoordinate =
            FlipVector2(new Vector2(sourceRect.X / (float)tex.Width, sourceRect.Y / (float)tex.Height), flip);
        mappedData.VertexPtr[1].TextureCoordinate =
            FlipVector2(new Vector2(sourceRect.Right / (float)tex.Width, sourceRect.Y / (float)tex.Height), flip);
        mappedData.VertexPtr[2].TextureCoordinate =
            FlipVector2(new Vector2(sourceRect.Right / (float)tex.Width, sourceRect.Bottom / (float)tex.Height), flip);
        mappedData.VertexPtr[3].TextureCoordinate =
            FlipVector2(new Vector2(sourceRect.X / (float)tex.Width, sourceRect.Bottom / (float)tex.Height), flip);

        SetQuadIndices(mappedData);
    }

    public static unsafe void AllocateRotatedTexturedQuad(this Renderer renderer, VixieTexture tex, Vector2 position,
                                                          Vector2       scale, float rotation, Vector2 rotationOrigin,
                                                          Color         color, TextureFlip flip = TextureFlip.None) {
        Vector2 size = new(tex.Width * scale.X, tex.Height * scale.Y);

        MappedData mappedData = renderer.Reserve(4, 6);
        mappedData.VertexPtr[0] = new Vertex {
            Position          = Vector2.Zero,
            Color             = color,
            TexId             = renderer.GetTextureId(tex),
            TextureCoordinate = FlipVector2(new Vector2(0, 0), flip)
        };
        mappedData.VertexPtr[1] = new Vertex {
            Position = size with {
                Y = 0
            },
            Color             = color,
            TexId             = renderer.GetTextureId(tex),
            TextureCoordinate = FlipVector2(new Vector2(1, 0), flip)
        };
        mappedData.VertexPtr[2] = new Vertex {
            Position          = size,
            Color             = color,
            TexId             = renderer.GetTextureId(tex),
            TextureCoordinate = FlipVector2(new Vector2(1, 1), flip)
        };
        mappedData.VertexPtr[3] = new Vertex {
            Position = size with {
                X = 0
            },
            Color             = color,
            TexId             = renderer.GetTextureId(tex),
            TextureCoordinate = FlipVector2(new Vector2(0, 1), flip)
        };

        Matrix4x4 rotMat = Matrix4x4.CreateRotationZ(rotation);

        mappedData.VertexPtr[0].Position -= rotationOrigin;
        mappedData.VertexPtr[1].Position -= rotationOrigin;
        mappedData.VertexPtr[2].Position -= rotationOrigin;
        mappedData.VertexPtr[3].Position -= rotationOrigin;

        mappedData.VertexPtr[0].Position = Vector2.Transform(mappedData.VertexPtr[0].Position, rotMat);
        mappedData.VertexPtr[1].Position = Vector2.Transform(mappedData.VertexPtr[1].Position, rotMat);
        mappedData.VertexPtr[2].Position = Vector2.Transform(mappedData.VertexPtr[2].Position, rotMat);
        mappedData.VertexPtr[3].Position = Vector2.Transform(mappedData.VertexPtr[3].Position, rotMat);

        mappedData.VertexPtr[0].Position += position;
        mappedData.VertexPtr[1].Position += position;
        mappedData.VertexPtr[2].Position += position;
        mappedData.VertexPtr[3].Position += position;

        SetQuadIndices(mappedData);
    }

    public static unsafe void AllocateRotatedTexturedQuadWithSourceRect(this Renderer renderer, VixieTexture tex,
                                                                        Vector2        position,
                                                                        Vector2        scale, float rotation,
                                                                        Vector2        rotationOrigin,
                                                                        Rectangle      sourceRect, Color color, TextureFlip flip = TextureFlip.None) {
        Vector2 size = new(sourceRect.Width * scale.X, sourceRect.Height * scale.Y);
        
        if (tex.InternalFlip)
            sourceRect.Height *= -1;

        MappedData mappedData = renderer.Reserve(4, 6);
        mappedData.VertexPtr[0] = new Vertex {
            Position          = Vector2.Zero,
            Color             = color,
            TexId             = renderer.GetTextureId(tex),
            TextureCoordinate = FlipVector2(new Vector2(sourceRect.X / (float)tex.Width, sourceRect.Y / (float)tex.Height), flip)
        };
        mappedData.VertexPtr[1] = new Vertex {
            Position = size with {
                Y = 0
            },
            Color             = color,
            TexId             = renderer.GetTextureId(tex),
            TextureCoordinate = FlipVector2(new Vector2(sourceRect.Right / (float)tex.Width, sourceRect.Y / (float)tex.Height), flip)
        };
        mappedData.VertexPtr[2] = new Vertex {
            Position          = size,
            Color             = color,
            TexId             = renderer.GetTextureId(tex),
            TextureCoordinate = FlipVector2(new Vector2(sourceRect.Right / (float)tex.Width, sourceRect.Bottom / (float)tex.Height), flip)
        };
        mappedData.VertexPtr[3] = new Vertex {
            Position = size with {
                X = 0
            },
            Color             = color,
            TexId             = renderer.GetTextureId(tex),
            TextureCoordinate = FlipVector2(new Vector2(sourceRect.X / (float)tex.Width, sourceRect.Bottom / (float)tex.Height), flip)
        };

        Matrix4x4 rotMat = Matrix4x4.CreateRotationZ(rotation);

        mappedData.VertexPtr[0].Position -= rotationOrigin;
        mappedData.VertexPtr[1].Position -= rotationOrigin;
        mappedData.VertexPtr[2].Position -= rotationOrigin;
        mappedData.VertexPtr[3].Position -= rotationOrigin;

        mappedData.VertexPtr[0].Position = Vector2.Transform(mappedData.VertexPtr[0].Position, rotMat);
        mappedData.VertexPtr[1].Position = Vector2.Transform(mappedData.VertexPtr[1].Position, rotMat);
        mappedData.VertexPtr[2].Position = Vector2.Transform(mappedData.VertexPtr[2].Position, rotMat);
        mappedData.VertexPtr[3].Position = Vector2.Transform(mappedData.VertexPtr[3].Position, rotMat);

        mappedData.VertexPtr[0].Position += position;
        mappedData.VertexPtr[1].Position += position;
        mappedData.VertexPtr[2].Position += position;
        mappedData.VertexPtr[3].Position += position;

        SetQuadIndices(mappedData);
    }

    public static void DrawString(this Renderer renderer, DynamicSpriteFont font,
                                  string        text,     Vector2           position, Color   color,
                                  float         rotation, Vector2           scale,    Vector2 origin = default) {
        Guard.EnsureNonNull(renderer.FontRenderer, "renderer.FontRenderer");

        font.DrawText(renderer.FontRenderer, text, position,
                      System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B),
                      scale, rotation, origin);
    }
    public static void DrawString(this Renderer renderer, DynamicSpriteFont font, string text, Vector2 position, Color color) {
        Guard.EnsureNonNull(renderer.FontRenderer, "renderer.FontRenderer");
        
        font.DrawText(renderer.FontRenderer, text, position,
                      System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B));
    }
    public static void DrawString(this Renderer renderer, DynamicSpriteFont font, string text, Vector2 position, System.Drawing.Color[] colors, float rotation, Vector2? scale, Vector2 origin) {
        Guard.EnsureNonNull(renderer.FontRenderer, "renderer.FontRenderer");

        font.DrawText(renderer.FontRenderer, text, position, colors, scale, rotation, origin);
    }
}