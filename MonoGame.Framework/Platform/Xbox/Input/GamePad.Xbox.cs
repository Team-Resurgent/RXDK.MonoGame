// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using RxdkInput = Rxdk;

namespace Microsoft.Xna.Framework.Input
{
    /// <summary>
    /// The four controller ports, over Rxdk.Input.
    ///
    /// The original controller predates the 360 pad XNA was designed around, and differs in two
    /// ways that matter here. Its A, B, X, Y, Black, and White buttons are analog, reported as a
    /// byte of pressure rather than a bit, so they are thresholded. There are no shoulder buttons:
    /// Black and White sit where the 360 put LB and RB, and that is how they are reported, which is
    /// what a game checking Buttons.LeftShoulder expects to find under its thumb.
    /// </summary>
    static partial class GamePad
    {
        /// <summary>Half travel. An analog button is "down" well before it bottoms out.</summary>
        const byte AnalogButtonThreshold = 0x40;

        static readonly RxdkInput.GamePad[] _pads = new RxdkInput.GamePad[4];

        private static int PlatformGetMaxNumberOfGamePads()
        {
            return 4;
        }

        /// <summary>
        /// Opens the port on first use and reopens it when a controller is plugged in later. A
        /// closed port costs one connection-mask read per call, which is cheap enough to do every
        /// frame and is the only way hot-plugging can be noticed.
        /// </summary>
        static RxdkInput.GamePad GetPad(int index)
        {
            if (index < 0 || index >= _pads.Length)
                return null;

            var pad = _pads[index];
            if (pad != null && pad.IsConnected)
                return pad;

            if ((RxdkInput.GamePad.ConnectedPorts & (1u << index)) == 0)
                return null;

            if (pad != null)
                pad.Dispose();
            pad = RxdkInput.GamePad.Open(index);
            _pads[index] = pad;
            return pad.IsConnected ? pad : null;
        }

        private static GamePadCapabilities PlatformGetCapabilities(int index)
        {
            var caps = new GamePadCapabilities();
            if (GetPad(index) == null)
                return caps;

            caps.IsConnected = true;
            caps.DisplayName = "Xbox Controller";
            caps.Identifier = "Xbox";
            caps.GamePadType = GamePadType.GamePad;
            caps.HasAButton = true;
            caps.HasBButton = true;
            caps.HasXButton = true;
            caps.HasYButton = true;
            caps.HasStartButton = true;
            caps.HasBackButton = true;
            caps.HasDPadUpButton = true;
            caps.HasDPadDownButton = true;
            caps.HasDPadLeftButton = true;
            caps.HasDPadRightButton = true;
            // Black and White stand in for the shoulder buttons.
            caps.HasLeftShoulderButton = true;
            caps.HasRightShoulderButton = true;
            caps.HasLeftStickButton = true;
            caps.HasRightStickButton = true;
            caps.HasLeftXThumbStick = true;
            caps.HasLeftYThumbStick = true;
            caps.HasRightXThumbStick = true;
            caps.HasRightYThumbStick = true;
            caps.HasLeftTrigger = true;
            caps.HasRightTrigger = true;
            caps.HasLeftVibrationMotor = true;
            caps.HasRightVibrationMotor = true;
            return caps;
        }

        private static GamePadState PlatformGetState(int index, GamePadDeadZone leftDeadZoneMode, GamePadDeadZone rightDeadZoneMode)
        {
            var pad = GetPad(index);
            if (pad == null)
                return GamePadState.Default;

            var raw = pad.GetState();
            if (!raw.IsConnected)
                return GamePadState.Default;

            Buttons buttons = 0;
            if (raw.A >= AnalogButtonThreshold) buttons |= Buttons.A;
            if (raw.B >= AnalogButtonThreshold) buttons |= Buttons.B;
            if (raw.X >= AnalogButtonThreshold) buttons |= Buttons.X;
            if (raw.Y >= AnalogButtonThreshold) buttons |= Buttons.Y;
            if (raw.White >= AnalogButtonThreshold) buttons |= Buttons.LeftShoulder;
            if (raw.Black >= AnalogButtonThreshold) buttons |= Buttons.RightShoulder;
            if (raw.IsDown(RxdkInput.GamePadButton.Start)) buttons |= Buttons.Start;
            if (raw.IsDown(RxdkInput.GamePadButton.Back)) buttons |= Buttons.Back;
            if (raw.IsDown(RxdkInput.GamePadButton.LeftThumb)) buttons |= Buttons.LeftStick;
            if (raw.IsDown(RxdkInput.GamePadButton.RightThumb)) buttons |= Buttons.RightStick;

            var dPad = new GamePadDPad(
                raw.IsDown(RxdkInput.GamePadButton.DPadUp) ? ButtonState.Pressed : ButtonState.Released,
                raw.IsDown(RxdkInput.GamePadButton.DPadDown) ? ButtonState.Pressed : ButtonState.Released,
                raw.IsDown(RxdkInput.GamePadButton.DPadLeft) ? ButtonState.Pressed : ButtonState.Released,
                raw.IsDown(RxdkInput.GamePadButton.DPadRight) ? ButtonState.Pressed : ButtonState.Released);

            var sticks = new GamePadThumbSticks(
                new Vector2(Axis(raw.ThumbLeftX), Axis(raw.ThumbLeftY)),
                new Vector2(Axis(raw.ThumbRightX), Axis(raw.ThumbRightY)),
                leftDeadZoneMode, rightDeadZoneMode);

            var triggers = new GamePadTriggers(raw.LeftTrigger / 255.0f, raw.RightTrigger / 255.0f);

            var state = new GamePadState(sticks, triggers, new GamePadButtons(buttons), dPad);
            state.PacketNumber = (int)raw.PacketNumber;
            return state;
        }

        /// <summary>
        /// A signed 16-bit axis to -1..1. Negative full scale is one step further from zero than
        /// positive, so dividing by 32767 would let a stick pushed fully left read below -1.
        /// </summary>
        static float Axis(short value)
        {
            float scaled = value / 32767.0f;
            return scaled < -1.0f ? -1.0f : scaled;
        }

        private static bool PlatformSetVibration(int index, float leftMotor, float rightMotor, float leftTrigger, float rightTrigger)
        {
            var pad = GetPad(index);
            if (pad == null)
                return false;

            // The controller has no trigger motors; those arguments belong to later hardware.
            pad.SetVibration(Motor(leftMotor), Motor(rightMotor));
            return true;
        }

        static ushort Motor(float value)
        {
            if (value <= 0.0f)
                return 0;
            if (value >= 1.0f)
                return ushort.MaxValue;
            return (ushort)(value * ushort.MaxValue);
        }
    }
}
