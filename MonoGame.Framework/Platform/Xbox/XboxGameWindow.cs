// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using XDevice = Rxdk.GraphicsDevice;

namespace Microsoft.Xna.Framework
{
    /// <summary>
    /// The console's single full-screen surface. Its bounds are whatever video mode the runtime
    /// negotiated with the dashboard settings and the attached cable, so they are read from the
    /// device rather than requested.
    /// </summary>
    class XboxGameWindow : GameWindow
    {
        readonly XboxGamePlatform _platform;

        public XboxGameWindow(XboxGamePlatform platform)
        {
            _platform = platform;
        }

        public override bool AllowUserResizing
        {
            get { return false; }
            set { }
        }

        public override Rectangle ClientBounds
        {
            get
            {
                return new Rectangle(0, 0, XDevice.Width, XDevice.Height);
            }
        }

        public override Point Position
        {
            get { return Point.Zero; }
            set { }
        }

        public override DisplayOrientation CurrentOrientation
        {
            get { return DisplayOrientation.LandscapeLeft; }
        }

        /// <summary>There is no window handle; nothing on the console takes one.</summary>
        public override IntPtr Handle
        {
            get { return IntPtr.Zero; }
        }

        public override string ScreenDeviceName
        {
            get { return "Xbox"; }
        }

        public override void BeginScreenDeviceChange(bool willBeFullScreen)
        {
        }

        public override void EndScreenDeviceChange(string screenDeviceName, int clientWidth, int clientHeight)
        {
        }

        protected internal override void SetSupportedOrientations(DisplayOrientation orientations)
        {
        }

        protected override void SetTitle(string title)
        {
            // A title bar needs a window manager.
        }
    }
}
