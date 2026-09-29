// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class VertexBuffer
    {
        internal Rxdk.VertexBuffer Buffer;

        private void PlatformConstruct()
        {
            Buffer = Rxdk.VertexBuffer.Create(VertexDeclaration.VertexStride * VertexCount);
        }

        /// <summary>
        /// The console cannot lose its device, so a buffer's contents survive for as long as the
        /// buffer does and there is nothing to recreate.
        /// </summary>
        private void PlatformGraphicsDeviceResetting()
        {
        }

        private void PlatformGetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount, int vertexStride)
            where T : struct
        {
            throw new NotSupportedException(
                "Vertex buffers on the Xbox are write-only; keep a copy of the data if you need to read it back.");
        }

        private void PlatformSetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount,
            int vertexStride, SetDataOptions options, int bufferSize, int elementSizeInBytes) where T : struct
        {
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                var source = handle.AddrOfPinnedObject() + startIndex * elementSizeInBytes;
                if (vertexStride == elementSizeInBytes)
                {
                    // Tightly packed, so one copy does it.
                    Buffer.SetData(source, offsetInBytes, elementCount * elementSizeInBytes);
                }
                else
                {
                    // A stride wider than the element means the caller is filling one field of each
                    // vertex and the rest of the buffer must be left alone, so copy row by row.
                    for (int i = 0; i < elementCount; i++)
                        Buffer.SetData(source + i * elementSizeInBytes,
                            offsetInBytes + i * vertexStride, elementSizeInBytes);
                }
            }
            finally
            {
                handle.Free();
            }
        }

        private void PlatformDispose()
        {
            if (Buffer != null)
            {
                Buffer.Dispose();
                Buffer = null;
            }
        }
    }
}
