// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Runtime.InteropServices;
using DS = Rxdk.Audio;

namespace Microsoft.Xna.Framework.Audio
{
    /// <summary>
    /// The console's DirectSound device, shared by every sound. It is opened on first use, so a
    /// title that never plays a sound never starts the audio processor.
    /// </summary>
    static class XboxAudio
    {
        // dsound.h
        internal const int DSBCAPS_LOCDEFER = 0x00040000;
        internal const int DSBPLAY_LOOPING = 0x00000001;
        internal const int DSBSTATUS_PLAYING = 0x00000001;
        internal const int DSBSTATUS_PAUSED = 0x00000002;
        internal const int DSBVOLUME_MIN = -10000;
        internal const int DSBFREQUENCY_MIN = 188;
        internal const int DSBFREQUENCY_MAX = 191983;
        internal const int DSMIXBIN_FRONT_LEFT = 0;
        internal const int DSMIXBIN_FRONT_RIGHT = 1;

        // DSBUFFERDESC: dwSize, dwFlags, dwBufferBytes, lpwfxFormat, lpMixBins, dwInputMixBin.
        internal const int BufferDescSize = 24;

        // WAVEFORMATEX, including cbSize.
        internal const int WaveFormatSize = 18;

        static IntPtr _device;
        static bool _unavailable;

        /// <summary>The device, or zero if DirectSound could not be started.</summary>
        internal static unsafe IntPtr Device
        {
            get
            {
                if (_device == IntPtr.Zero && !_unavailable)
                {
                    IntPtr device;
                    int hr = DS.DirectSoundCreate(IntPtr.Zero, (IntPtr)(&device), IntPtr.Zero);
                    if (hr < 0 || device == IntPtr.Zero)
                        _unavailable = true;
                    else
                        _device = device;
                }
                return _device;
            }
        }

        /// <summary>
        /// DirectSound on the console has no thread of its own. Voices that finish are only
        /// reclaimed, and status only moves on, when the title calls this, so the game loop calls
        /// it once a frame.
        /// </summary>
        internal static void DoWork()
        {
            if (_device != IntPtr.Zero)
                DS.DirectSoundDoWork();
        }

        /// <summary>A 0-to-1 gain as the hundredths of a decibel of attenuation DirectSound takes.</summary>
        internal static int ToMillibels(float gain)
        {
            if (gain <= 0.0001f)
                return DSBVOLUME_MIN;
            if (gain >= 1f)
                return 0;
            int mb = (int)(2000.0 * Math.Log10(gain));
            return mb < DSBVOLUME_MIN ? DSBVOLUME_MIN : mb;
        }

        /// <summary>A native WAVEFORMATEX for 8 or 16 bit PCM. Freed with Marshal.FreeHGlobal.</summary>
        internal static IntPtr CreatePcmFormat(int channels, int sampleRate, int bitsPerSample)
        {
            var format = Marshal.AllocHGlobal(WaveFormatSize);
            int blockAlign = channels * bitsPerSample / 8;
            Marshal.WriteInt16(format, 0, 1);
            Marshal.WriteInt16(format, 2, (short)channels);
            Marshal.WriteInt32(format, 4, sampleRate);
            Marshal.WriteInt32(format, 8, sampleRate * blockAlign);
            Marshal.WriteInt16(format, 12, (short)blockAlign);
            Marshal.WriteInt16(format, 14, (short)bitsPerSample);
            Marshal.WriteInt16(format, 16, 0);
            return format;
        }
    }
}
