// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

namespace Microsoft.Xna.Framework.Graphics
{
    public partial class OcclusionQuery
    {
        /// <summary>
        /// The console does have occlusion queries, but they are read through a fence rather than a
        /// query object and nothing in the framework needs them yet, so this refuses clearly rather
        /// than reporting a pixel count that is always zero.
        /// </summary>
        private void PlatformConstruct()
        {
            throw new NotSupportedException(
                "Occlusion queries are not implemented in the Xbox backend.");
        }

        private void PlatformBegin()
        {
        }

        private void PlatformEnd()
        {
        }

        private bool PlatformGetResult(out int pixelCount)
        {
            pixelCount = 0;
            return false;
        }
    }
}
