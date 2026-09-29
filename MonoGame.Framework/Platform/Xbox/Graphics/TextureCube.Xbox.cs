// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// The console samples cube maps, and Rxdk.Graphics can load one from a packed resource, but
    /// building one face by face from content is not wired up yet.
    /// </summary>
    public partial class TextureCube : Texture
    {
        private void PlatformConstruct(GraphicsDevice graphicsDevice, int size, bool mipMap,
            SurfaceFormat format, bool renderTarget)
        {
            throw new NotSupportedException("TextureCube is not implemented in the Xbox backend.");
        }

        private void PlatformGetData<T>(CubeMapFace cubeMapFace, int level, Rectangle rect,
            T[] data, int startIndex, int elementCount) where T : struct
        {
            throw new NotSupportedException("TextureCube is not implemented in the Xbox backend.");
        }

        private void PlatformSetData<T>(CubeMapFace face, int level, Rectangle rect,
            T[] data, int startIndex, int elementCount)
        {
            throw new NotSupportedException("TextureCube is not implemented in the Xbox backend.");
        }
    }
}
