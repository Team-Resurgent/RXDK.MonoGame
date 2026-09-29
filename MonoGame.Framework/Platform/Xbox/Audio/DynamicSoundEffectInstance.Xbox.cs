// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

namespace Microsoft.Xna.Framework.Audio
{
    /// <summary>
    /// Xbox dynamic sound buffers. Silent, but buffers are consumed immediately so a game that
    /// streams audio keeps making progress and its BufferNeeded event keeps firing.
    /// </summary>
    public sealed partial class DynamicSoundEffectInstance : SoundEffectInstance
    {
        private void PlatformCreate()
        {
        }

        private int PlatformGetPendingBufferCount()
        {
            return 0;
        }

        private new void PlatformPlay()
        {
        }

        private new void PlatformPause()
        {
        }

        private new void PlatformResume()
        {
        }

        private void PlatformStop()
        {
        }

        private void PlatformSubmitBuffer(byte[] buffer, int offset, int count)
        {
        }

        private new void PlatformDispose(bool disposing)
        {
        }

        private void PlatformUpdateQueue()
        {
            // Nothing is queued, so every submitted buffer has already been "played".
            CheckBufferCount();
        }
    }
}
