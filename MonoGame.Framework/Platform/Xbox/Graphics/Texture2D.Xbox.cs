// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.IO;
using System.Runtime.InteropServices;
using MonoGame.Framework.Utilities;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class Texture2D : Texture
    {
        private void PlatformConstruct(int width, int height, bool mipmap, SurfaceFormat format, SurfaceType type, bool shared)
        {
            // A render target builds its own surface in RenderTarget2D.Xbox.cs.
            if (type == SurfaceType.RenderTarget)
                return;

            var surface = XboxFormat.RequireTextureFormat(format);
            var powerOfTwo = IsPowerOfTwo(width) && IsPowerOfTwo(height);
            var gpuWidth = width;
            var gpuHeight = height;

            if (Rxdk.Texture.BlockBytes(surface) != 0)
            {
                // A compressed texture has no linear variant to fall back on: the block grid is the
                // layout, and the GPU only addresses it for power-of-two sides.
                if (!powerOfTwo)
                    throw new NotSupportedException(
                        "A " + width + "x" + height + " " + format + " texture is not a size this GPU "
                        + "can sample compressed. Resize the image to powers of two, or build it as "
                        + "Color.");
            }
            else if (!powerOfTwo)
            {
                // A linear texture is addressed in texels, so a 0-to-1 sprite coordinate misses it,
                // and it cannot wrap. Stretch onto a power-of-two swizzled surface instead, which
                // the sampler reads from 0 to 1. Mipmaps would each need the same stretch, which
                // this path does not build.
                if (_levelCount > 1)
                    throw new NotSupportedException(
                        "A " + width + "x" + height + " texture cannot be mipmapped on this GPU, which "
                        + "only swizzles power-of-two sizes. Resize the image or turn mipmaps off.");

                gpuWidth = NextPowerOfTwo(width);
                gpuHeight = NextPowerOfTwo(height);
            }

            _texture = Rxdk.Texture.Create(gpuWidth, gpuHeight, _levelCount, surface);
        }

        /// <summary>
        /// Whether this texture's bytes are DXT blocks, which are passed through as they are rather
        /// than having their channels reordered.
        /// </summary>
        bool IsCompressed
        {
            get { return Rxdk.Texture.BlockBytes(XboxFormat.Texture(_format)) != 0; }
        }

        static bool IsPowerOfTwo(int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }

        static int NextPowerOfTwo(int value)
        {
            var padded = 1;
            while (padded < value)
            {
                padded <<= 1;
                if (padded > 4096)
                    throw new NotSupportedException(
                        "A texture side of " + value + " exceeds the 4096 texel limit once padded "
                        + "to a power of two.");
            }
            return padded;
        }

        int GpuLevelWidth(int level)
        {
            return Math.Max(1, _texture.Width >> level);
        }

        int GpuLevelHeight(int level)
        {
            return Math.Max(1, _texture.Height >> level);
        }

        private void PlatformSetData<T>(int level, T[] data, int startIndex, int elementCount) where T : struct
        {
            int levelWidth, levelHeight;
            GetSizeForLevel(width, height, level, out levelWidth, out levelHeight);
            WriteLevel(level, data, startIndex, elementCount, levelWidth, levelHeight);
        }

        private void PlatformSetData<T>(int level, int arraySlice, Rectangle rect, T[] data, int startIndex, int elementCount)
            where T : struct
        {
            int levelWidth, levelHeight;
            GetSizeForLevel(width, height, level, out levelWidth, out levelHeight);

            // A full-level rectangle is the common case and is just a level write. A partial one
            // would need a locked sub-rect with a source pitch, which the backend does not do yet.
            if (rect.X != 0 || rect.Y != 0 || rect.Width != levelWidth || rect.Height != levelHeight)
                throw new NotSupportedException(
                    "The Xbox backend writes whole mip levels; a partial rectangle is not supported yet.");

            WriteLevel(level, data, startIndex, elementCount, levelWidth, levelHeight);
        }

        void WriteLevel<T>(int level, T[] data, int startIndex, int elementCount, int levelWidth, int levelHeight)
            where T : struct
        {
            var elementSize = ReflectionHelpers.SizeOf<T>.Get();
            var bytes = new byte[elementCount * elementSize];
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                Marshal.Copy(handle.AddrOfPinnedObject() + startIndex * elementSize, bytes, 0, bytes.Length);
            }
            finally
            {
                handle.Free();
            }

            if (!IsCompressed)
                XboxFormat.SwapRedAndBlue(bytes);

            // The surface may be larger than the image so both sides are powers of two. Stretch the
            // image over it with nearest sampling: every surface texel takes the image texel under
            // its centre, so a point sample at an image texel's centre reads that texel back.
            var gpuWidth = GpuLevelWidth(level);
            var gpuHeight = GpuLevelHeight(level);
            if (gpuWidth != levelWidth || gpuHeight != levelHeight)
            {
                var stretched = new byte[gpuWidth * gpuHeight * 4];
                for (int y = 0; y < gpuHeight; y++)
                {
                    var sy = (int)((y + 0.5) * levelHeight / gpuHeight);
                    for (int x = 0; x < gpuWidth; x++)
                    {
                        var sx = (int)((x + 0.5) * levelWidth / gpuWidth);
                        Buffer.BlockCopy(bytes, (sy * levelWidth + sx) * 4, stretched, (y * gpuWidth + x) * 4, 4);
                    }
                }
                bytes = stretched;
                levelWidth = gpuWidth;
                levelHeight = gpuHeight;
            }

            _texture.SetData(level, bytes, 0, levelWidth, levelHeight);
        }

        private void PlatformGetData<T>(int level, int arraySlice, Rectangle rect, T[] data, int startIndex, int elementCount)
            where T : struct
        {
            int levelWidth, levelHeight;
            GetSizeForLevel(width, height, level, out levelWidth, out levelHeight);
            if (rect.X != 0 || rect.Y != 0 || rect.Width != levelWidth || rect.Height != levelHeight)
                throw new NotSupportedException(
                    "The Xbox backend reads whole mip levels; a partial rectangle is not supported yet.");

            var elementSize = ReflectionHelpers.SizeOf<T>.Get();
            byte[] bytes;
            if (GpuLevelWidth(level) == levelWidth && GpuLevelHeight(level) == levelHeight)
            {
                bytes = new byte[elementCount * elementSize];
                _texture.GetData(level, bytes, 0, levelWidth, levelHeight);
            }
            else
            {
                // The surface is the image stretched to a power of two. Read that, then sample each
                // image texel's centre back out of it.
                var gpuWidth = _texture.Width;
                var gpuHeight = _texture.Height;
                var gpuBytes = new byte[gpuWidth * gpuHeight * 4];
                _texture.GetData(level, gpuBytes, 0, gpuWidth, gpuHeight);
                bytes = new byte[levelWidth * levelHeight * 4];
                for (int y = 0; y < levelHeight; y++)
                {
                    var gy = (int)((y + 0.5) * gpuHeight / levelHeight);
                    for (int x = 0; x < levelWidth; x++)
                    {
                        var gx = (int)((x + 0.5) * gpuWidth / levelWidth);
                        Buffer.BlockCopy(gpuBytes, (gy * gpuWidth + gx) * 4, bytes, (y * levelWidth + x) * 4, 4);
                    }
                }
            }
            if (!IsCompressed)
                XboxFormat.SwapRedAndBlue(bytes);

            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                Marshal.Copy(bytes, 0, handle.AddrOfPinnedObject() + startIndex * elementSize, bytes.Length);
            }
            finally
            {
                handle.Free();
            }
        }

        private void PlatformReload(Stream textureStream)
        {
            throw new NotSupportedException(
                "Reloading a texture from a stream needs an image decoder the Xbox backend does not have.");
        }
    }
}
