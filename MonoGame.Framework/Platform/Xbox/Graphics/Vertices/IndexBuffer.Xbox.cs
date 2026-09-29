// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Runtime.InteropServices;
using MonoGame.Framework.Utilities;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class IndexBuffer
    {
        internal Rxdk.IndexBuffer Buffer;

        private void PlatformConstruct(IndexElementSize indexElementSize, int indexCount)
        {
            if (indexElementSize == IndexElementSize.ThirtyTwoBits)
                throw new NotSupportedException(
                    "The Xbox GPU reads 16-bit indices only; use IndexElementSize.SixteenBits.");

            Buffer = Rxdk.IndexBuffer.Create(indexCount * 2);
        }

        private void PlatformGraphicsDeviceResetting()
        {
        }

        private void PlatformGetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount)
            where T : struct
        {
            throw new NotSupportedException(
                "Index buffers on the Xbox are write-only; keep a copy of the data if you need to read it back.");
        }

        private void PlatformSetData<T>(int offsetInBytes, T[] data, int startIndex, int elementCount,
            SetDataOptions options) where T : struct
        {
            var elementSize = ReflectionHelpers.SizeOf<T>.Get();
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                Buffer.SetData(handle.AddrOfPinnedObject() + startIndex * elementSize,
                    offsetInBytes, elementCount * elementSize);
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
