// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class RenderTargetCube
    {
        private void PlatformConstruct(GraphicsDevice graphicsDevice, bool mipMap,
            DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage)
        {
            throw new NotSupportedException("RenderTargetCube is not implemented in the Xbox backend.");
        }

        Rxdk.Surface IRenderTarget.GetColorSurface()
        {
            throw new NotSupportedException("RenderTargetCube is not implemented in the Xbox backend.");
        }

        Rxdk.Surface IRenderTarget.GetDepthStencilSurface()
        {
            throw new NotSupportedException("RenderTargetCube is not implemented in the Xbox backend.");
        }
    }
}
