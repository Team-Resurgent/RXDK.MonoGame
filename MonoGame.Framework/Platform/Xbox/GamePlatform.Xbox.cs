// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

namespace Microsoft.Xna.Framework
{
    partial class GamePlatform
    {
        internal static GamePlatform PlatformCreate(Game game)
        {
            return new XboxGamePlatform(game);
        }
    }

    /// <summary>
    /// The original Xbox has one screen, one resolution chosen at boot, and no window manager, so
    /// most of the GamePlatform surface is fixed. There is also no message pump to service, which
    /// makes the run loop nothing more than ticking the game until it asks to exit.
    /// </summary>
    class XboxGamePlatform : GamePlatform
    {
        readonly XboxGameWindow _window;
        bool _exiting;

        public XboxGamePlatform(Game game)
            : base(game)
        {
            _window = new XboxGameWindow(this);
            Window = _window;
        }

        public override GameRunBehavior DefaultRunBehavior
        {
            get { return GameRunBehavior.Synchronous; }
        }

        public override void RunLoop()
        {
            while (!_exiting)
            {
                Game.Tick();
                Media.Song.Update();
                Audio.XboxAudio.DoWork();
            }
        }

        /// <summary>
        /// Only an asynchronous platform, one driven by an OS event loop, needs this. The console
        /// gives the title the CPU outright, so RunLoop is the only way in.
        /// </summary>
        public override void StartRunLoop()
        {
            throw new NotSupportedException(
                "The Xbox platform runs synchronously; GameRunBehavior.Asynchronous is not available.");
        }

        public override void Exit()
        {
            _exiting = true;
        }

        /// <summary>
        /// Swaps the finished frame onto the screen. Game.EndDraw asks the platform rather than the
        /// device to do this, and the base implementation does nothing, so without this every frame
        /// is drawn and then discarded.
        /// </summary>
        public override void Present()
        {
            var device = Game.GraphicsDevice;
            if (device != null)
                device.Present();
        }

        public override bool BeforeUpdate(GameTime gameTime)
        {
            return true;
        }

        public override bool BeforeDraw(GameTime gameTime)
        {
            return true;
        }

        public override void EnterFullScreen()
        {
            // Always full screen.
        }

        public override void ExitFullScreen()
        {
            // There is no windowed mode to return to.
        }

        public override void BeginScreenDeviceChange(bool willBeFullScreen)
        {
        }

        public override void EndScreenDeviceChange(string screenDeviceName, int clientWidth, int clientHeight)
        {
        }

        public override void Log(string message)
        {
            Console.WriteLine(message);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
