// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// Volume textures exist on the console, but nothing in the framework's 2D path uses one and
    /// they need their own swizzle and box-copy handling, so they are left unimplemented rather
    /// than half done.
    /// </summary>
    public partial class Texture3D : Texture
    {
        private void PlatformConstruct(GraphicsDevice graphicsDevice, int width, int height, int depth,
            bool mipMap, SurfaceFormat format, bool renderTarget)
        {
            throw new NotSupportedException("Texture3D is not implemented in the Xbox backend.");
        }

        private void PlatformSetData<T>(int level, int left, int top, int right, int bottom, int front, int back,
            T[] data, int startIndex, int elementCount)
        {
            throw new NotSupportedException("Texture3D is not implemented in the Xbox backend.");
        }

        private void PlatformGetData<T>(int level, int left, int top, int right, int bottom, int front, int back,
            T[] data, int startIndex, int elementCount)
        {
            throw new NotSupportedException("Texture3D is not implemented in the Xbox backend.");
        }
    }
}
