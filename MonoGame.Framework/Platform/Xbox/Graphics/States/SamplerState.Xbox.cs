// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using XDevice = Rxdk.GraphicsDevice;
using XStage = Rxdk.TextureStageState;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class SamplerState
    {
        internal void PlatformApplyState(GraphicsDevice device, int stage)
        {
            Rxdk.TextureFilter mag, min, mip;
            XboxFormat.Filters(Filter, out mag, out min, out mip);

            XDevice.SetTextureStageState(stage, XStage.MagFilter, (int)mag);
            XDevice.SetTextureStageState(stage, XStage.MinFilter, (int)min);
            XDevice.SetTextureStageState(stage, XStage.MipFilter, (int)mip);
            XDevice.SetTextureStageState(stage, XStage.MaxAnisotropy, MaxAnisotropy);
            XDevice.SetTextureStageState(stage, XStage.MaxMipLevel, MaxMipLevel);

            XDevice.SetTextureStageState(stage, XStage.AddressU, (int)XboxFormat.Address(AddressU));
            XDevice.SetTextureStageState(stage, XStage.AddressV, (int)XboxFormat.Address(AddressV));
            XDevice.SetTextureStageState(stage, XStage.AddressW, (int)XboxFormat.Address(AddressW));
        }
    }
}
