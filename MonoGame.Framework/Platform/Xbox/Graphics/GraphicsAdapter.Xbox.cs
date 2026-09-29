// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using XDevice = Rxdk.GraphicsDevice;

namespace Microsoft.Xna.Framework.Graphics
{
    partial class GraphicsAdapter
    {
        /// <summary>
        /// One adapter with one mode. The console's video mode comes from its dashboard settings
        /// and the cable that is plugged in, and the title cannot enumerate alternatives, so the
        /// mode reported here is the one already running.
        /// </summary>
        private static void PlatformInitializeAdapters(out ReadOnlyCollection<GraphicsAdapter> adapters)
        {
            if (!XDevice.Open())
                throw new InvalidOperationException(
                    "Could not open the Xbox display device (status " + XDevice.LastStatus + ").");

            var mode = new DisplayMode(XDevice.Width, XDevice.Height, SurfaceFormat.Color);

            var adapter = new GraphicsAdapter();
            adapter.DeviceName = "Xbox";
            adapter.Description = "NV2A";
            adapter._currentDisplayMode = mode;
            adapter._supportedDisplayModes = new DisplayModeCollection(new List<DisplayMode> { mode });

            adapters = new ReadOnlyCollection<GraphicsAdapter>(new List<GraphicsAdapter> { adapter });
        }

        /// <summary>
        /// The GPU is a shader model 1 part, which is below every profile XNA defines, but Reach is
        /// the closest description of what it can do and refusing it would leave nothing usable.
        /// </summary>
        private bool PlatformIsProfileSupported(GraphicsProfile graphicsProfile)
        {
            return graphicsProfile == GraphicsProfile.Reach;
        }
    }
}
