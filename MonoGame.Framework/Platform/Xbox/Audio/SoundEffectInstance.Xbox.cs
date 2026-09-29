// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using DS = Rxdk.Audio;

namespace Microsoft.Xna.Framework.Audio
{
    /// <summary>
    /// Xbox sound effect instances. Each one owns a DirectSound buffer that plays straight out of
    /// its SoundEffect's native samples, so an instance costs a voice, not a copy of the sound.
    ///
    /// The buffer is created with LOCDEFER, which holds a hardware voice only while it plays. A
    /// sound DirectSound cannot play (its format has no decoder, or the device did not start)
    /// keeps the old silent behaviour: the transport state is tracked, and a one-shot reports
    /// Stopped straight away so SoundEffectInstancePool recycles it.
    /// </summary>
    public partial class SoundEffectInstance : IDisposable
    {
        SoundEffect _source;
        IntPtr _buffer;
        SoundState _silentState = SoundState.Stopped;
        bool _looped;
        float _gain = 1f;
        float _panValue;
        float _pitchValue;

        internal void PlatformInitialize(SoundEffect source)
        {
            if (_source != source)
                ReleaseBuffer();
            _source = source;
        }

        internal void InitializeSound()
        {
        }

        unsafe bool EnsureBuffer()
        {
            if (_buffer != IntPtr.Zero)
                return true;
            if (_source == null || _source.NativeData == IntPtr.Zero)
                return false;
            var device = XboxAudio.Device;
            if (device == IntPtr.Zero)
                return false;

            // DSBUFFERDESC. Zero bytes, because SetBufferData points it at the effect's samples.
            int* desc = stackalloc int[XboxAudio.BufferDescSize / 4];
            desc[0] = XboxAudio.BufferDescSize;
            desc[1] = XboxAudio.DSBCAPS_LOCDEFER;
            desc[2] = 0;
            desc[3] = (int)_source.NativeFormat;
            desc[4] = 0;
            desc[5] = 0;

            IntPtr buffer;
            if (DS.IDirectSound_CreateSoundBuffer(device, (IntPtr)desc, (IntPtr)(&buffer), IntPtr.Zero) < 0
                || buffer == IntPtr.Zero)
                return false;
            if (DS.IDirectSoundBuffer_SetBufferData(buffer, _source.NativeData, _source.NativeDataSize) < 0)
            {
                DS.IDirectSoundBuffer_Release(buffer);
                return false;
            }
            if (_source.LoopLengthBytes > 0)
                DS.IDirectSoundBuffer_SetLoopRegion(buffer, _source.LoopStartBytes, _source.LoopLengthBytes);

            _buffer = buffer;
            ApplyVolume();
            ApplyPitch();
            ApplyPan();
            return true;
        }

        void ReleaseBuffer()
        {
            if (_buffer == IntPtr.Zero)
                return;
            DS.IDirectSoundBuffer_Stop(_buffer);
            DS.IDirectSoundBuffer_Release(_buffer);
            _buffer = IntPtr.Zero;
        }

        void ApplyVolume()
        {
            if (_buffer != IntPtr.Zero)
                DS.IDirectSoundBuffer_SetVolume(_buffer, XboxAudio.ToMillibels(_gain));
        }

        void ApplyPitch()
        {
            if (_buffer == IntPtr.Zero)
                return;
            // Pitch is in octaves, -1 to 1.
            int frequency = (int)(_source.SampleRate * Math.Pow(2.0, _pitchValue));
            if (frequency < XboxAudio.DSBFREQUENCY_MIN)
                frequency = XboxAudio.DSBFREQUENCY_MIN;
            if (frequency > XboxAudio.DSBFREQUENCY_MAX)
                frequency = XboxAudio.DSBFREQUENCY_MAX;
            DS.IDirectSoundBuffer_SetFrequency(_buffer, frequency);
        }

        /// <summary>
        /// Pan is the balance between the front left and right mix bins, which is where DirectSound
        /// sends a mono or stereo buffer by default.
        /// </summary>
        unsafe void ApplyPan()
        {
            if (_buffer == IntPtr.Zero)
                return;
            float left = _panValue > 0f ? 1f - _panValue : 1f;
            float right = _panValue < 0f ? 1f + _panValue : 1f;

            // DSMIXBINVOLUMEPAIR[2] followed by DSMIXBINS { count, pointer to the pairs }.
            int* pairs = stackalloc int[4];
            pairs[0] = XboxAudio.DSMIXBIN_FRONT_LEFT;
            pairs[1] = XboxAudio.ToMillibels(left);
            pairs[2] = XboxAudio.DSMIXBIN_FRONT_RIGHT;
            pairs[3] = XboxAudio.ToMillibels(right);
            int* mixBins = stackalloc int[2];
            mixBins[0] = 2;
            mixBins[1] = (int)pairs;
            DS.IDirectSoundBuffer_SetMixBinVolumes(_buffer, (IntPtr)mixBins);
        }

        private void PlatformApply3D(AudioListener listener, AudioEmitter emitter)
        {
        }

        private void PlatformPause()
        {
            if (_buffer != IntPtr.Zero)
                DS.IDirectSoundBuffer_Pause(_buffer, 1);
            else if (_silentState == SoundState.Playing)
                _silentState = SoundState.Paused;
        }

        private void PlatformPlay()
        {
            if (EnsureBuffer())
            {
                DS.IDirectSoundBuffer_SetCurrentPosition(_buffer, 0);
                DS.IDirectSoundBuffer_Play(_buffer, 0, 0, _looped ? XboxAudio.DSBPLAY_LOOPING : 0);
            }
            else
                _silentState = SoundState.Playing;
        }

        private void PlatformResume()
        {
            if (_buffer != IntPtr.Zero)
                DS.IDirectSoundBuffer_Pause(_buffer, 0);
            else if (_silentState == SoundState.Paused)
                _silentState = SoundState.Playing;
        }

        private void PlatformStop(bool immediate)
        {
            if (_buffer != IntPtr.Zero)
                DS.IDirectSoundBuffer_Stop(_buffer);
            _silentState = SoundState.Stopped;
        }

        private void FreeSource()
        {
            ReleaseBuffer();
        }

        private void PlatformSetIsLooped(bool value)
        {
            _looped = value;
        }

        private bool PlatformGetIsLooped()
        {
            return _looped;
        }

        private void PlatformSetPan(float value)
        {
            _panValue = value;
            ApplyPan();
        }

        private void PlatformSetPitch(float value)
        {
            _pitchValue = value;
            ApplyPitch();
        }

        private unsafe SoundState PlatformGetState()
        {
            if (_buffer != IntPtr.Zero)
            {
                int status = 0;
                if (DS.IDirectSoundBuffer_GetStatus(_buffer, (IntPtr)(&status)) < 0)
                    return SoundState.Stopped;
                if ((status & XboxAudio.DSBSTATUS_PAUSED) != 0)
                    return SoundState.Paused;
                if ((status & XboxAudio.DSBSTATUS_PLAYING) != 0)
                    return SoundState.Playing;
                return SoundState.Stopped;
            }

            if (_silentState == SoundState.Playing && !_looped)
                _silentState = SoundState.Stopped;
            return _silentState;
        }

        private void PlatformSetVolume(float value)
        {
            _gain = value;
            ApplyVolume();
        }

        internal void PlatformSetReverbMix(float mix)
        {
        }

        void ApplyReverb()
        {
        }

        void ApplyFilter()
        {
        }

        internal void PlatformSetFilter(FilterMode mode, float filterQ, float frequency)
        {
        }

        internal void PlatformClearFilter()
        {
        }

        internal void PlatformClearBuffer()
        {
        }

        private void PlatformDispose(bool disposing)
        {
            ReleaseBuffer();
            _silentState = SoundState.Stopped;
        }
    }
}
