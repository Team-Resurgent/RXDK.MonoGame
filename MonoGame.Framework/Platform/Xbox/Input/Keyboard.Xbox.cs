// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using RxdkInput = Rxdk;

namespace Microsoft.Xna.Framework.Input
{
    /// <summary>
    /// A USB keyboard, if one is plugged in.
    ///
    /// The console reports keystrokes as a queue of press and release events, while XNA asks for a
    /// snapshot of everything currently held. So the queue is drained on every GetState call and
    /// folded into a set of held keys. A game that polls once per frame therefore sees keys it
    /// pressed and released within that frame as never having been pressed, which is the same
    /// compromise every event-driven keyboard backend makes.
    /// </summary>
    static partial class Keyboard
    {
        static RxdkInput.Keyboard _keyboard;
        static bool _unavailable;
        static readonly List<Keys> _down = new List<Keys>();
        static bool _capsLock;
        static bool _numLock;

        private static KeyboardState PlatformGetState()
        {
            if (_keyboard == null)
            {
                if (_unavailable)
                    return new KeyboardState(_down, _capsLock, _numLock);
                try
                {
                    _keyboard = RxdkInput.Keyboard.Open();
                }
                catch (InvalidOperationException)
                {
                    // No keyboard queue. Keep reporting an empty state rather than retrying.
                    _unavailable = true;
                    return new KeyboardState(_down, _capsLock, _numLock);
                }
            }

            RxdkInput.Keystroke stroke;
            while (_keyboard.TryGetKeystroke(out stroke))
            {
                // XNA's Keys values are the Windows virtual key codes the console also reports.
                var key = (Keys)stroke.VirtualKey;
                if (stroke.IsKeyUp)
                {
                    _down.Remove(key);
                    if (key == Keys.CapsLock)
                        _capsLock = !_capsLock;
                    else if (key == Keys.NumLock)
                        _numLock = !_numLock;
                }
                else if (!stroke.IsRepeat && !_down.Contains(key))
                {
                    _down.Add(key);
                }
            }

            return new KeyboardState(_down, _capsLock, _numLock);
        }
    }
}
