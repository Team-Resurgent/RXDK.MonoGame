// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

namespace Microsoft.Xna.Framework.Graphics
{
    public sealed partial class SamplerStateCollection
    {
        /// <summary>
        /// Direct3D 8 has no sampler objects to pre-create, so a change is just a note to write the
        /// stage states again on the next draw.
        /// </summary>
        private void PlatformSetSamplerState(int index)
        {
        }

        private void PlatformClear()
        {
        }

        private void PlatformDirty()
        {
        }

        internal void PlatformSetSamplers(GraphicsDevice device)
        {
            for (var i = 0; i < _actualSamplers.Length; i++)
            {
                var sampler = _actualSamplers[i];
                if (sampler != null)
                    sampler.PlatformApplyState(device, i);
            }
        }
    }
}
