// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using XDevice = Rxdk.GraphicsDevice;
using XState = Rxdk.RenderState;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class DepthStencilState
    {
        internal void PlatformApplyState(GraphicsDevice device)
        {
            XDevice.SetRenderState(XState.ZEnable, DepthBufferEnable ? 1 : 0);
            XDevice.SetRenderState(XState.ZWriteEnable, DepthBufferWriteEnable ? 1 : 0);
            if (DepthBufferEnable)
                XDevice.SetRenderState(XState.ZFunc, XboxFormat.Compare(DepthBufferFunction));

            XDevice.SetRenderState(XState.StencilEnable, StencilEnable ? 1 : 0);
            if (!StencilEnable)
                return;

            // The console's stencil unit is single sided, so a two-sided state would quietly test
            // back faces with the front face's operations.
            if (TwoSidedStencilMode)
                throw new NotSupportedException(
                    "The Xbox GPU has one stencil face; TwoSidedStencilMode is not available.");

            XDevice.SetRenderState(XState.StencilFunc, XboxFormat.Compare(StencilFunction));
            XDevice.SetRenderState(XState.StencilFail, XboxFormat.StencilOp(StencilFail));
            XDevice.SetRenderState(XState.StencilZFail, XboxFormat.StencilOp(StencilDepthBufferFail));
            XDevice.SetRenderState(XState.StencilPass, XboxFormat.StencilOp(StencilPass));
            XDevice.SetRenderState(XState.StencilRef, ReferenceStencil);
            XDevice.SetRenderState(XState.StencilMask, StencilMask);
            XDevice.SetRenderState(XState.StencilWriteMask, StencilWriteMask);
        }
    }
}
