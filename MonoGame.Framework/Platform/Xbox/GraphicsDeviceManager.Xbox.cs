// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework
{
    public partial class GraphicsDeviceManager
    {
        /// <summary>
        /// Replaces the back buffer size a title asked for with the one the console actually gave
        /// it. The display is one of a handful of television modes, so a preference is a request at
        /// best; leaving it in place would set a viewport larger than the surface being drawn into,
        /// which the GPU will not rasterise, and would scale every projection built from the
        /// viewport by the wrong amount.
        ///
        /// The display has to be open for its mode to be known, and this runs the first time before
        /// any graphics device exists. Opening it here is safe and does not pre-empt anything: the
        /// call is idempotent, and which mode the console uses is settled by the dashboard rather
        /// than by anything the title or the device passes in.
        /// </summary>
        partial void PlatformPreparePresentationParameters(PresentationParameters presentationParameters)
        {
            if (!Rxdk.GraphicsDevice.Open())
                return;

            presentationParameters.BackBufferWidth = Rxdk.GraphicsDevice.Width;
            presentationParameters.BackBufferHeight = Rxdk.GraphicsDevice.Height;
        }
    }
}
