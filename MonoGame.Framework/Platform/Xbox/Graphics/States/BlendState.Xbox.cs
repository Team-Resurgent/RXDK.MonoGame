// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using XDevice = Rxdk.GraphicsDevice;
using XState = Rxdk.RenderState;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class BlendState
    {
        /// <summary>
        /// The console blends with one set of render states for the whole target, so only the first
        /// of XNA's four independent render target blend descriptions is used.
        /// </summary>
        internal void PlatformApplyState(GraphicsDevice device)
        {
            var enabled = !(ColorSourceBlend == Blend.One
                            && ColorDestinationBlend == Blend.Zero
                            && AlphaSourceBlend == Blend.One
                            && AlphaDestinationBlend == Blend.Zero);

            XDevice.SetRenderState(XState.AlphaBlendEnable, enabled ? 1 : 0);
            if (!enabled)
                return;

            // The console has no separate alpha blend factors, so the colour ones govern both
            // channels. Every stock XNA BlendState sets them the same way; one that does not is
            // better refused than silently drawn wrong.
            if (ColorSourceBlend != AlphaSourceBlend
                || ColorDestinationBlend != AlphaDestinationBlend
                || ColorBlendFunction != AlphaBlendFunction)
                throw new NotSupportedException(
                    "The Xbox GPU blends colour and alpha with one set of factors; a BlendState " +
                    "with different alpha factors cannot be applied.");

            XDevice.SetRenderState(XState.SrcBlend, XboxFormat.Blend(ColorSourceBlend));
            XDevice.SetRenderState(XState.DestBlend, XboxFormat.Blend(ColorDestinationBlend));
            XDevice.SetRenderState(XState.BlendOp, XboxFormat.BlendOp(ColorBlendFunction));

            var factor = BlendFactor;
            XDevice.SetRenderState(XState.BlendColor,
                (factor.A << 24) | (factor.R << 16) | (factor.G << 8) | factor.B);

            XDevice.SetRenderState(XState.ColorWriteEnable, (int)ColorWriteChannelsMask(ColorWriteChannels));
        }

        static Rxdk.ColorWriteEnable ColorWriteChannelsMask(ColorWriteChannels channels)
        {
            Rxdk.ColorWriteEnable mask = 0;
            if ((channels & ColorWriteChannels.Red) != 0) mask |= Rxdk.ColorWriteEnable.Red;
            if ((channels & ColorWriteChannels.Green) != 0) mask |= Rxdk.ColorWriteEnable.Green;
            if ((channels & ColorWriteChannels.Blue) != 0) mask |= Rxdk.ColorWriteEnable.Blue;
            if ((channels & ColorWriteChannels.Alpha) != 0) mask |= Rxdk.ColorWriteEnable.Alpha;
            return mask;
        }
    }
}
