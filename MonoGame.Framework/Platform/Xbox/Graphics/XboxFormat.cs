// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using Rxdk;

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// Translation between XNA's enums and the console's Direct3D 8 values.
    ///
    /// The console's Direct3D predates programmable blend state objects, so most of what XNA models
    /// as an immutable state object becomes a handful of render state writes.
    /// </summary>
    static class XboxFormat
    {
        /// <summary>
        /// The uncompressed format a texture is created in. The console's swizzled A8R8G8B8 is the
        /// one uncompressed layout the whole backend agrees on, so SurfaceFormat.Color is the only
        /// uncompressed format content may arrive as.
        /// </summary>
        public const Rxdk.SurfaceFormat TextureFormat = Rxdk.SurfaceFormat.A8R8G8B8;

        /// <summary>
        /// The console's format for an XNA texture format, or Unknown if there is not one. The GPU
        /// samples DXT directly, so compressed content is handed over untouched rather than being
        /// expanded, which matters on a machine with 64 MB to share with everything else.
        /// </summary>
        public static Rxdk.SurfaceFormat Texture(SurfaceFormat format)
        {
            switch (format)
            {
                case SurfaceFormat.Color: return TextureFormat;
                case SurfaceFormat.Dxt1:
                case SurfaceFormat.Dxt1a:
                case SurfaceFormat.Dxt1SRgb: return Rxdk.SurfaceFormat.Dxt1;
                case SurfaceFormat.Dxt3:
                case SurfaceFormat.Dxt3SRgb: return Rxdk.SurfaceFormat.Dxt3;
                case SurfaceFormat.Dxt5:
                case SurfaceFormat.Dxt5SRgb: return Rxdk.SurfaceFormat.Dxt5;
                default: return Rxdk.SurfaceFormat.Unknown;
            }
        }

        /// <summary>
        /// The first vertex shader constant register holding a texture coordinate scale. One
        /// register per sampler follows it, so a shader reads stage i's scale from c(TexCoordScale
        /// + i). This sits just past BasicEffect's constant buffer, the larger of the two stock
        /// effects, which ends at c25. Must match TEXCOORD_SCALE in tools/mgfx-xbox.py.
        /// </summary>
        public const int TexCoordScaleRegister = 26;

        static readonly float[] _texCoordScale = new float[4];

        /// <summary>
        /// Publishes the scale a shader must apply to its 0-to-1 texture coordinates before
        /// sampling stage <paramref name="stage"/>.
        ///
        /// A swizzled texture fills its surface, so the scale stays 1; a non-power-of-two image is
        /// stretched to fill one too, so wrap addressing repeats the image and not its surface. A
        /// linear texture, such as a render target, is addressed in texels, so the scale is its
        /// size. Doing it here rather than in the coordinates themselves keeps it invisible to the
        /// rest of the framework.
        /// </summary>
        public static void SetTexCoordScale(int stage, Texture texture)
        {
            var su = 1f;
            var sv = 1f;
            var native = texture == null ? null : texture._texture;
            if (native != null && native.IsLinear)
            {
                su = native.Width;
                sv = native.Height;
            }

            _texCoordScale[0] = su;
            _texCoordScale[1] = sv;
            _texCoordScale[2] = 0f;
            _texCoordScale[3] = 1f;
            Rxdk.GraphicsDevice.SetVertexShaderConstant(TexCoordScaleRegister + stage, _texCoordScale);
        }

        public static Rxdk.SurfaceFormat RequireTextureFormat(SurfaceFormat format)
        {
            var mapped = Texture(format);
            if (mapped == Rxdk.SurfaceFormat.Unknown)
                throw new NotSupportedException(
                    "The Xbox backend has no texture format for SurfaceFormat." + format +
                    ". Content has to be built as Color or one of the DXT formats.");

            return mapped;
        }

        /// <summary>
        /// Trades the red and blue bytes of every pixel, in place.
        ///
        /// XNA's SurfaceFormat.Color puts red in the low byte and D3DFMT_A8R8G8B8 puts blue there,
        /// so the two names describe opposite layouts and texture data has to be turned around.
        /// The vertex colour path avoids this with a shader swizzle, but a texture is sampled by
        /// fixed hardware that has no such hook. DXT blocks are never passed through here: the
        /// channel order lives inside the block, which the GPU decodes for itself.
        /// </summary>
        public static void SwapRedAndBlue(byte[] pixels)
        {
            for (var i = 0; i + 3 < pixels.Length; i += 4)
            {
                var red = pixels[i];
                pixels[i] = pixels[i + 2];
                pixels[i + 2] = red;
            }
        }

        public static int BytesPerPixel(SurfaceFormat format)
        {
            switch (format)
            {
                case SurfaceFormat.Color:
                case SurfaceFormat.Bgra32:
                case SurfaceFormat.Bgr32:
                case SurfaceFormat.Rgba1010102:
                case SurfaceFormat.Rg32:
                case SurfaceFormat.Single:
                    return 4;
                case SurfaceFormat.Bgr565:
                case SurfaceFormat.Bgra5551:
                case SurfaceFormat.Bgra4444:
                case SurfaceFormat.NormalizedByte2:
                case SurfaceFormat.HalfSingle:
                    return 2;
                case SurfaceFormat.Alpha8:
                    return 1;
                default:
                    throw new NotSupportedException(
                        "The Xbox backend cannot handle SurfaceFormat." + format +
                        ". Build content as Color, or add a conversion for it.");
            }
        }

        public static Rxdk.PrimitiveType Primitive(PrimitiveType type)
        {
            switch (type)
            {
                case PrimitiveType.TriangleList: return Rxdk.PrimitiveType.TriangleList;
                case PrimitiveType.TriangleStrip: return Rxdk.PrimitiveType.TriangleStrip;
                case PrimitiveType.LineList: return Rxdk.PrimitiveType.LineList;
                case PrimitiveType.LineStrip: return Rxdk.PrimitiveType.LineStrip;
                case PrimitiveType.PointList: return Rxdk.PrimitiveType.PointList;
                default:
                    throw new NotSupportedException("Unsupported PrimitiveType." + type);
            }
        }

        public static Rxdk.TextureAddress Address(TextureAddressMode mode)
        {
            switch (mode)
            {
                case TextureAddressMode.Wrap: return Rxdk.TextureAddress.Wrap;
                case TextureAddressMode.Mirror: return Rxdk.TextureAddress.Mirror;
                case TextureAddressMode.Border: return Rxdk.TextureAddress.Border;
                default: return Rxdk.TextureAddress.Clamp;
            }
        }

        /// <summary>
        /// Direct3D 8 splits filtering into magnification, minification, and mip filters, where XNA
        /// names the combination. Anisotropic is honoured for minification only, which is where it
        /// does the work.
        /// </summary>
        public static void Filters(TextureFilter filter,
            out Rxdk.TextureFilter mag, out Rxdk.TextureFilter min, out Rxdk.TextureFilter mip)
        {
            switch (filter)
            {
                case TextureFilter.Point:
                    mag = Rxdk.TextureFilter.Point; min = Rxdk.TextureFilter.Point; mip = Rxdk.TextureFilter.Point;
                    break;
                case TextureFilter.Anisotropic:
                    mag = Rxdk.TextureFilter.Linear; min = Rxdk.TextureFilter.Anisotropic; mip = Rxdk.TextureFilter.Linear;
                    break;
                case TextureFilter.LinearMipPoint:
                    mag = Rxdk.TextureFilter.Linear; min = Rxdk.TextureFilter.Linear; mip = Rxdk.TextureFilter.Point;
                    break;
                case TextureFilter.PointMipLinear:
                    mag = Rxdk.TextureFilter.Point; min = Rxdk.TextureFilter.Point; mip = Rxdk.TextureFilter.Linear;
                    break;
                case TextureFilter.MinLinearMagPointMipLinear:
                    mag = Rxdk.TextureFilter.Point; min = Rxdk.TextureFilter.Linear; mip = Rxdk.TextureFilter.Linear;
                    break;
                case TextureFilter.MinLinearMagPointMipPoint:
                    mag = Rxdk.TextureFilter.Point; min = Rxdk.TextureFilter.Linear; mip = Rxdk.TextureFilter.Point;
                    break;
                case TextureFilter.MinPointMagLinearMipLinear:
                    mag = Rxdk.TextureFilter.Linear; min = Rxdk.TextureFilter.Point; mip = Rxdk.TextureFilter.Linear;
                    break;
                case TextureFilter.MinPointMagLinearMipPoint:
                    mag = Rxdk.TextureFilter.Linear; min = Rxdk.TextureFilter.Point; mip = Rxdk.TextureFilter.Point;
                    break;
                default:
                    mag = Rxdk.TextureFilter.Linear; min = Rxdk.TextureFilter.Linear; mip = Rxdk.TextureFilter.Linear;
                    break;
            }
        }

        /// <summary>D3DBLEND_*. Only the modes the console's combiner implements.</summary>
        /// <summary>
        /// D3DBLEND_*. These are the Xbox values, not the desktop ordinals. The desktop numbering
        /// (Zero = 1, SrcAlpha = 5, and so on) falls in a hole the driver rejects, so the factors
        /// stayed at the default of One and Zero and a transparent texel was written as black.
        /// </summary>
        public static int Blend(Blend blend)
        {
            switch (blend)
            {
                case Microsoft.Xna.Framework.Graphics.Blend.Zero: return 0;
                case Microsoft.Xna.Framework.Graphics.Blend.One: return 1;
                case Microsoft.Xna.Framework.Graphics.Blend.SourceColor: return 0x300;
                case Microsoft.Xna.Framework.Graphics.Blend.InverseSourceColor: return 0x301;
                case Microsoft.Xna.Framework.Graphics.Blend.SourceAlpha: return 0x302;
                case Microsoft.Xna.Framework.Graphics.Blend.InverseSourceAlpha: return 0x303;
                case Microsoft.Xna.Framework.Graphics.Blend.DestinationAlpha: return 0x304;
                case Microsoft.Xna.Framework.Graphics.Blend.InverseDestinationAlpha: return 0x305;
                case Microsoft.Xna.Framework.Graphics.Blend.DestinationColor: return 0x306;
                case Microsoft.Xna.Framework.Graphics.Blend.InverseDestinationColor: return 0x307;
                case Microsoft.Xna.Framework.Graphics.Blend.SourceAlphaSaturation: return 0x308;
                default: return 1;
            }
        }

        /// <summary>D3DBLENDOP_*.</summary>
        public static int BlendOp(BlendFunction function)
        {
            switch (function)
            {
                case BlendFunction.Add: return 0x8006;
                case BlendFunction.Subtract: return 0x800a;
                case BlendFunction.ReverseSubtract: return 0x800b;
                case BlendFunction.Min: return 0x8007;
                case BlendFunction.Max: return 0x8008;
                default: return 0x8006;
            }
        }

        /// <summary>D3DCMP_*, shared by the depth, stencil, and alpha tests.</summary>
        public static int Compare(CompareFunction function)
        {
            switch (function)
            {
                case CompareFunction.Never: return 0x200;
                case CompareFunction.Less: return 0x201;
                case CompareFunction.Equal: return 0x202;
                case CompareFunction.LessEqual: return 0x203;
                case CompareFunction.Greater: return 0x204;
                case CompareFunction.NotEqual: return 0x205;
                case CompareFunction.GreaterEqual: return 0x206;
                default: return 0x207;
            }
        }

        /// <summary>D3DSTENCILOP_*.</summary>
        public static int StencilOp(StencilOperation operation)
        {
            switch (operation)
            {
                case StencilOperation.Keep: return 0x1e00;
                case StencilOperation.Zero: return 0;
                case StencilOperation.Replace: return 0x1e01;
                case StencilOperation.IncrementSaturation: return 0x1e02;
                case StencilOperation.DecrementSaturation: return 0x1e03;
                case StencilOperation.Invert: return 0x150a;
                case StencilOperation.Increment: return 0x8507;
                case StencilOperation.Decrement: return 0x8508;
                default: return 0x1e00;
            }
        }

        /// <summary>
        /// XNA and Direct3D 8 agree on winding: counter-clockwise is front. The projection flips Y,
        /// which reverses the winding of everything drawn, so a mapping that inverted these would
        /// cull every sprite.
        /// </summary>
        public static Cull Cull(CullMode mode)
        {
            switch (mode)
            {
                case CullMode.CullClockwiseFace: return Rxdk.Cull.Clockwise;
                case CullMode.CullCounterClockwiseFace: return Rxdk.Cull.CounterClockwise;
                default: return Rxdk.Cull.None;
            }
        }

        public static Fill Fill(FillMode mode)
        {
            return mode == FillMode.WireFrame ? Rxdk.Fill.Wireframe : Rxdk.Fill.Solid;
        }

        public static uint PackColor(Vector4 color)
        {
            return ((uint)Clamp255(color.W) << 24)
                 | ((uint)Clamp255(color.X) << 16)
                 | ((uint)Clamp255(color.Y) << 8)
                 | (uint)Clamp255(color.Z);
        }

        static int Clamp255(float value)
        {
            int scaled = (int)(value * 255.0f + 0.5f);
            if (scaled < 0) return 0;
            return scaled > 255 ? 255 : scaled;
        }
    }
}
