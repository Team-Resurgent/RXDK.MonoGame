// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using XDevice = Rxdk.GraphicsDevice;
using XState = Rxdk.RenderState;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class RasterizerState
    {
        internal void PlatformApplyState(GraphicsDevice device)
        {
            XDevice.SetRenderState(XState.CullMode, (int)XboxFormat.Cull(CullMode));
            XDevice.SetRenderState(XState.FillMode, (int)XboxFormat.Fill(FillMode));
            XDevice.SetRenderState(XState.MultiSampleAntiAlias, MultiSampleAntiAlias ? 1 : 0);

            // Depth bias on the console is a polygon offset, enabled separately from the amount.
            var biased = DepthBias != 0.0f || SlopeScaleDepthBias != 0.0f;
            XDevice.SetRenderState(XState.SolidOffsetEnable, biased ? 1 : 0);
            if (biased)
            {
                XDevice.SetRenderState(XState.PolygonOffsetZOffset, FloatBits(DepthBias));
                XDevice.SetRenderState(XState.PolygonOffsetZSlopeScale, FloatBits(SlopeScaleDepthBias));
            }
        }

        /// <summary>
        /// A handful of render states hold a float rather than an integer, and Direct3D 8 passes
        /// them as the raw bit pattern in a DWORD.
        /// </summary>
        static unsafe int FloatBits(float value)
        {
            return *(int*)&value;
        }
    }
}
