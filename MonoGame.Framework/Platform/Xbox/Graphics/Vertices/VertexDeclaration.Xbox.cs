// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// Builds the Xbox vertex shader declaration: the token stream that tells the GPU which stream
    /// bytes feed which vertex shader input register.
    /// </summary>
    static class XboxVertexDeclaration
    {
        // D3DVSD_* token encoding. A stream token opens a stream, a stream-data token either binds
        // the next bytes to an input register or skips them, and the array ends with D3DVSD_END.
        // The token type is the top three bits: D3DVSD_TOKEN_STREAM is 1, D3DVSD_TOKEN_STREAMDATA
        // is 2, and the type shift is 29.
        const uint TokenStream = 1u << 29;
        const uint TokenStreamData = 2u << 29;

        // Within a stream-data token, D3DVSD_DATATYPESHIFT and D3DVSD_SKIPCOUNTSHIFT are both 16,
        // the register sits in the low bits, and bit 28 marks the token as a skip rather than a bind.
        const int DataTypeShift = 16;
        const int SkipCountShift = 16;
        const uint SkipFlag = 0x10000000;

        const uint End = 0xFFFFFFFF;

        /// <summary>
        /// Uses the vertex buffer currently bound on slot zero, because that is the layout the
        /// shader will read and Direct3D 8 binds the two together at shader creation time.
        /// </summary>
        public static byte[] Build(GraphicsDevice device, VertexAttribute[] attributes)
        {
            var declaration = device.GetBoundVertexDeclaration();
            if (declaration == null)
                throw new InvalidOperationException(
                    "A vertex buffer must be set before the vertex shader is applied, because the " +
                    "Xbox binds a shader to the vertex layout it reads.");

            var tokens = new List<uint>();
            tokens.Add(TokenStream | 0);

            var elements = declaration.InternalVertexElements;
            var offset = 0;
            for (var i = 0; i < elements.Length; i++)
            {
                var element = elements[i];

                // A gap between elements has to be skipped explicitly; the GPU walks the stream in
                // declaration order rather than seeking to each element's offset.
                if (element.Offset > offset)
                    tokens.Add(Skip((element.Offset - offset) / 4));
                else if (element.Offset < offset)
                    throw new NotSupportedException(
                        "The Xbox reads vertex elements in stream order; this declaration has an " +
                        "element that starts before the previous one ends.");

                var size = SizeOf(element.VertexElementFormat);
                var register = RegisterFor(element, attributes);
                if (register < 0)
                    tokens.Add(Skip(size / 4));
                else
                    tokens.Add(TokenStreamData | ((uint)DataType(element.VertexElementFormat) << DataTypeShift)
                        | (uint)register);
                offset = element.Offset + size;
            }

            if (declaration.VertexStride > offset)
                tokens.Add(Skip((declaration.VertexStride - offset) / 4));

            tokens.Add(End);

            var bytes = new byte[tokens.Count * 4];
            for (var i = 0; i < tokens.Count; i++)
                BitConverter.GetBytes(tokens[i]).CopyTo(bytes, i * 4);
            return bytes;
        }

        /// <summary>D3DVSD_SKIP: advance the stream pointer without feeding a register.</summary>
        static uint Skip(int dwords)
        {
            return TokenStreamData | SkipFlag | ((uint)dwords << SkipCountShift);
        }

        /// <summary>
        /// The shader's own attribute list says which register each usage was assigned, which is how
        /// a declaration built here lines up with a shader assembled elsewhere. Returns -1 for an
        /// element the shader does not read, which is ordinary: BasicEffect with TextureEnabled off
        /// still draws a vertex that carries texture coordinates.
        /// </summary>
        static int RegisterFor(VertexElement element, VertexAttribute[] attributes)
        {
            for (var i = 0; i < attributes.Length; i++)
            {
                if (attributes[i].usage == element.VertexElementUsage
                    && attributes[i].index == element.UsageIndex)
                    return attributes[i].location;
            }

            return -1;
        }

        /// <summary>D3DVSDT_*.</summary>
        static int DataType(VertexElementFormat format)
        {
            switch (format)
            {
                case VertexElementFormat.Single: return 0x12;
                case VertexElementFormat.Vector2: return 0x22;
                case VertexElementFormat.Vector3: return 0x32;
                case VertexElementFormat.Vector4: return 0x42;
                case VertexElementFormat.Color: return 0x40;
                case VertexElementFormat.Byte4: return 0x40;
                case VertexElementFormat.Short2: return 0x25;
                case VertexElementFormat.Short4: return 0x45;
                default:
                    throw new NotSupportedException(
                        "The Xbox cannot read VertexElementFormat." + format + " from a stream.");
            }
        }

        static int SizeOf(VertexElementFormat format)
        {
            switch (format)
            {
                case VertexElementFormat.Single: return 4;
                case VertexElementFormat.Vector2: return 8;
                case VertexElementFormat.Vector3: return 12;
                case VertexElementFormat.Vector4: return 16;
                case VertexElementFormat.Color: return 4;
                case VertexElementFormat.Byte4: return 4;
                case VertexElementFormat.Short2: return 4;
                case VertexElementFormat.Short4: return 8;
                default:
                    throw new NotSupportedException(
                        "The Xbox cannot read VertexElementFormat." + format + " from a stream.");
            }
        }
    }
}
