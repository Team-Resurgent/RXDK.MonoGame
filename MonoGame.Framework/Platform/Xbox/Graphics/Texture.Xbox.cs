// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

namespace Microsoft.Xna.Framework.Graphics
{
    public abstract partial class Texture
    {
        /// <summary>
        /// The Direct3D texture. Every texture kind the console supports keeps its handle here so
        /// TextureCollection can bind any of them without knowing which it has.
        /// </summary>
        internal Rxdk.Texture _texture;

        private void PlatformGraphicsDeviceResetting()
        {
            // The console never loses its device, so there is nothing to release and rebuild.
        }

        private void PlatformDispose(bool disposing)
        {
            if (disposing && _texture != null)
            {
                _texture.Dispose();
                _texture = null;
            }
        }
    }
}
