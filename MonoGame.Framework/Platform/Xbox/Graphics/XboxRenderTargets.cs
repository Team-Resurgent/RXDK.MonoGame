// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using XDevice = Rxdk.GraphicsDevice;

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// The render target part of the Xbox backend that does not belong to any one class.
    ///
    /// Direct3D 8 binds surfaces rather than views, and the back buffer's own surfaces belong to
    /// the device, so they are captured once at startup to be restored when a game stops drawing
    /// into a render target.
    /// </summary>
    static class XboxRenderTargets
    {
        static Rxdk.Surface _backBuffer;
        static Rxdk.Surface _backDepth;
        static bool _captured;

        static void Capture()
        {
            if (_captured)
                return;
            _backBuffer = XDevice.GetRenderTarget();
            _backDepth = XDevice.GetDepthStencil();
            _captured = true;
        }

        public static void BindBackBuffer()
        {
            Capture();
            XDevice.SetRenderTarget(_backBuffer, _backDepth);
        }

        public static void Bind(IRenderTarget target)
        {
            Capture();
            // A render target without its own depth buffer borrows the back buffer's, which is the
            // same size or larger, so depth testing keeps working inside the target.
            var depth = target.GetDepthStencilSurface();
            XDevice.SetRenderTarget(target.GetColorSurface(), depth ?? _backDepth);
        }
    }

    /// <summary>
    /// What the Xbox backend needs from any render target, whatever texture kind backs it.
    /// </summary>
    internal partial interface IRenderTarget
    {
        Rxdk.Surface GetColorSurface();

        /// <summary>The target's own depth buffer, or null to keep the back buffer's.</summary>
        Rxdk.Surface GetDepthStencilSurface();
    }
}
