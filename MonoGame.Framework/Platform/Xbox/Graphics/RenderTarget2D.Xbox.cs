// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class RenderTarget2D
    {
        Rxdk.Surface _colorSurface;
        Rxdk.Surface _depthSurface;

        private void PlatformConstruct(GraphicsDevice graphicsDevice, int width, int height, bool mipMap,
            DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage, bool shared)
        {
            // The texture is what a later draw call samples; the surface is what the GPU draws into.
            _texture = Rxdk.Texture.Create(width, height, 1, XboxFormat.TextureFormat);
            _colorSurface = Rxdk.Surface.FromTexture(_texture, 0);

            if (preferredDepthFormat != DepthFormat.None)
                _depthSurface = Rxdk.Surface.CreateDepthStencil(width, height, DepthSurfaceFormat(preferredDepthFormat));
        }

        /// <summary>
        /// The console has no depth-only 24-bit format, so Depth24 and Depth24Stencil8 both land on
        /// D24S8 and a game asking for the former simply gets a stencil buffer it does not use.
        /// </summary>
        static Rxdk.SurfaceFormat DepthSurfaceFormat(DepthFormat format)
        {
            return format == DepthFormat.Depth16
                ? Rxdk.SurfaceFormat.Depth16
                : Rxdk.SurfaceFormat.Depth24Stencil8;
        }

        private void PlatformGraphicsDeviceResetting()
        {
        }

        Rxdk.Surface IRenderTarget.GetColorSurface()
        {
            return _colorSurface;
        }

        Rxdk.Surface IRenderTarget.GetDepthStencilSurface()
        {
            return _depthSurface;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_colorSurface != null)
                {
                    _colorSurface.Dispose();
                    _colorSurface = null;
                }
                if (_depthSurface != null)
                {
                    _depthSurface.Dispose();
                    _depthSurface = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
