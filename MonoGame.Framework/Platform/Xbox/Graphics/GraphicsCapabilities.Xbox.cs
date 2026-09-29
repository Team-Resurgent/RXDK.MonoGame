// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

namespace Microsoft.Xna.Framework.Graphics
{
    internal partial class GraphicsCapabilities
    {
        /// <summary>
        /// The NV2A is fixed hardware, so every one of these is a constant rather than something to
        /// query. It samples DXT1 through DXT5 and can filter anisotropically up to 4 taps, but it
        /// has no float textures, no texture arrays, no vertex texturing, and no instancing.
        /// </summary>
        private void PlatformInitialize(GraphicsDevice device)
        {
            SupportsNonPowerOfTwo = false;
            SupportsTextureFilterAnisotropic = true;
            MaxTextureAnisotropy = 4;
            SupportsDepth24 = true;
            SupportsPackedDepthStencil = true;
            SupportsDepthNonLinear = false;
            SupportsDxt1 = true;
            SupportsS3tc = true;
            SupportsPvrtc = false;
            SupportsEtc1 = false;
            SupportsEtc2 = false;
            SupportsAtitc = false;
            SupportsAstc = false;
            SupportsTextureMaxLevel = true;
            SupportsSRgb = false;
            SupportsTextureArrays = false;
            SupportsDepthClamp = false;
            SupportsVertexTextures = false;
            SupportsFloatTextures = false;
            SupportsHalfFloatTextures = false;
            SupportsNormalized = false;
            SupportsInstancing = false;
            SupportsBaseIndexInstancing = false;
            SupportsSeparateBlendStates = false;
        }
    }
}
