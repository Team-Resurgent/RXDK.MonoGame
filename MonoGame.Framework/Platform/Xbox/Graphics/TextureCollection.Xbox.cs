// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using XDevice = Rxdk.GraphicsDevice;

namespace Microsoft.Xna.Framework.Graphics
{
    public sealed partial class TextureCollection
    {
        void PlatformInit()
        {
        }

        /// <summary>
        /// Unbinds any texture that is about to be drawn into. Reading and writing the same surface
        /// in one draw is undefined on the console as it is everywhere else.
        /// </summary>
        internal void ClearTargets(GraphicsDevice device, RenderTargetBinding[] targets)
        {
            for (var i = 0; i < _textures.Length; i++)
            {
                if (_textures[i] == null)
                    continue;

                for (int k = 0; k < targets.Length; k++)
                {
                    if (_textures[i] == targets[k].RenderTarget)
                    {
                        _dirty &= ~(1 << i);
                        _textures[i] = null;
                        XDevice.SetTexture(i, (Rxdk.Texture)null);
                        break;
                    }
                }
            }
        }

        void PlatformClear()
        {
        }

        void PlatformSetTextures(GraphicsDevice device)
        {
            if (_dirty == 0)
                return;

            for (var i = 0; i < _textures.Length; i++)
            {
                var mask = 1 << i;
                if ((_dirty & mask) == 0)
                    continue;

                var texture = _textures[i];
                if (texture == null || texture.IsDisposed)
                {
                    XDevice.SetTexture(i, (Rxdk.Texture)null);
                    XboxFormat.SetTexCoordScale(i, null);
                }
                else
                {
                    XDevice.SetTexture(i, texture._texture);
                    XboxFormat.SetTexCoordScale(i, texture);
                    unchecked
                    {
                        _graphicsDevice._graphicsMetrics._textureCount++;
                    }
                }
            }

            _dirty = 0;
        }
    }
}
