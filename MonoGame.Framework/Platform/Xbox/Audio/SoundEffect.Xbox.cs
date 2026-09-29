// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Xna.Framework.Audio
{
    /// <summary>
    /// Xbox sound effects, played through DirectSound.
    ///
    /// The samples are copied once into native memory, which does not move, and every instance
    /// points its own DirectSound buffer at that one copy. DirectSound plays 8 and 16 bit PCM
    /// directly; 32 bit float is converted to 16 bit on load. The content pipeline's MS-ADPCM
    /// (Quality Medium or Low) and XACT wave banks have no decoder here, so those sounds load and
    /// stay silent rather than stopping the game.
    /// </summary>
    public sealed partial class SoundEffect : IDisposable
    {
        internal const int MAX_PLAYING_INSTANCES = 64;

        internal IntPtr NativeData;
        internal int NativeDataSize;
        internal IntPtr NativeFormat;
        internal int SampleRate;
        internal int BlockAlign;
        internal int LoopStartBytes;
        internal int LoopLengthBytes;

        private void PlatformLoadAudioStream(Stream stream, out TimeSpan duration)
        {
            using (var reader = new BinaryReader(stream))
            {
                if (new string(reader.ReadChars(4)) != "RIFF")
                    throw new ArgumentException("The stream is not a RIFF wave file.", "stream");
                reader.ReadInt32();
                if (new string(reader.ReadChars(4)) != "WAVE")
                    throw new ArgumentException("The stream is not a RIFF wave file.", "stream");

                int tag = 0, channels = 0, rate = 0, bits = 0;
                byte[] data = null;
                while (data == null && stream.Position + 8 <= stream.Length)
                {
                    var id = new string(reader.ReadChars(4));
                    int size = reader.ReadInt32();
                    if (id == "fmt ")
                    {
                        tag = reader.ReadInt16();
                        channels = reader.ReadInt16();
                        rate = reader.ReadInt32();
                        reader.ReadInt32();
                        reader.ReadInt16();
                        bits = reader.ReadInt16();
                        stream.Seek(size - 16, SeekOrigin.Current);
                    }
                    else if (id == "data")
                        data = reader.ReadBytes(size);
                    else
                        stream.Seek(size, SeekOrigin.Current);
                    if ((size & 1) != 0 && data == null)
                        stream.Seek(1, SeekOrigin.Current);
                }
                if (data == null)
                    throw new ArgumentException("The wave file has no data chunk.", "stream");

                Load(tag, data, 0, data.Length, rate, channels, bits, 0, 0);
                duration = DurationOf(data.Length, rate, channels, bits);
            }
        }

        private void PlatformInitializePcm(byte[] buffer, int offset, int count, int sampleBits, int sampleRate, AudioChannels channels, int loopStart, int loopLength)
        {
            Load(1, buffer, offset, count, sampleRate, (int)channels, sampleBits, loopStart, loopLength);
        }

        private void PlatformInitializeIeeeFloat(byte[] buffer, int offset, int count, int sampleRate, AudioChannels channels, int loopStart, int loopLength)
        {
            Load(3, buffer, offset, count, sampleRate, (int)channels, 32, loopStart, loopLength);
        }

        private void PlatformInitializeAdpcm(byte[] buffer, int offset, int count, int sampleRate, AudioChannels channels, int blockAlignment, int loopStart, int loopLength)
        {
        }

        private void PlatformInitializeIma4(byte[] buffer, int offset, int count, int sampleRate, AudioChannels channels, int blockAlignment, int loopStart, int loopLength)
        {
        }

        private void PlatformInitializeFormat(byte[] header, byte[] buffer, int bufferSize, int loopStart, int loopLength)
        {
            // WAVEFORMATEX: format tag at 0, channels at 2, samples/sec at 4, bits/sample at 14.
            int tag = BitConverter.ToInt16(header, 0);
            int channels = BitConverter.ToInt16(header, 2);
            int sampleRate = BitConverter.ToInt32(header, 4);
            int bits = header.Length >= 16 ? BitConverter.ToInt16(header, 14) : 16;
            Load(tag, buffer, 0, bufferSize, sampleRate, channels, bits, loopStart, loopLength);
        }

        private void PlatformInitializeXact(MiniFormatTag codec, byte[] buffer, int channels, int sampleRate, int blockAlignment, int loopStart, int loopLength, out TimeSpan duration)
        {
            if (codec == MiniFormatTag.Pcm)
            {
                int bits = blockAlignment / Math.Max(1, channels) * 8;
                Load(1, buffer, 0, buffer.Length, sampleRate, channels, bits, loopStart, loopLength);
                duration = DurationOf(buffer.Length, sampleRate, channels, bits);
            }
            else
                duration = DurationOf(buffer.Length, sampleRate, channels, 4);
        }

        /// <summary>
        /// Keeps PCM (tag 1) as it is and turns 32 bit float (tag 3) into 16 bit PCM. Anything else
        /// is left unloaded, which makes the sound silent.
        /// </summary>
        void Load(int tag, byte[] buffer, int offset, int count, int sampleRate, int channels, int bits, int loopStart, int loopLength)
        {
            if (channels < 1 || channels > 2 || sampleRate < XboxAudio.DSBFREQUENCY_MIN || count <= 0)
                return;

            byte[] pcm;
            if (tag == 1 && (bits == 8 || bits == 16))
            {
                pcm = buffer;
            }
            else if (tag == 3 && bits == 32)
            {
                int samples = count / 4;
                pcm = new byte[samples * 2];
                for (int i = 0; i < samples; i++)
                {
                    float f = BitConverter.ToSingle(buffer, offset + i * 4);
                    int s = (int)(f * 32767f);
                    s = s > short.MaxValue ? short.MaxValue : s < short.MinValue ? short.MinValue : s;
                    pcm[i * 2] = (byte)s;
                    pcm[i * 2 + 1] = (byte)(s >> 8);
                }
                offset = 0;
                count = pcm.Length;
                bits = 16;
            }
            else
                return;

            BlockAlign = channels * bits / 8;
            count -= count % BlockAlign;
            NativeData = Marshal.AllocHGlobal(count);
            Marshal.Copy(pcm, offset, NativeData, count);
            NativeDataSize = count;
            NativeFormat = XboxAudio.CreatePcmFormat(channels, sampleRate, bits);
            SampleRate = sampleRate;
            if (loopLength > 0 && (loopStart + loopLength) * BlockAlign <= count)
            {
                LoopStartBytes = loopStart * BlockAlign;
                LoopLengthBytes = loopLength * BlockAlign;
            }
        }

        static TimeSpan DurationOf(int byteCount, int sampleRate, int channels, int bitsPerSample)
        {
            int bytesPerSecond = sampleRate * channels * Math.Max(1, bitsPerSample / 8);
            if (bytesPerSecond <= 0)
                return TimeSpan.Zero;
            return TimeSpan.FromSeconds((double)byteCount / bytesPerSecond);
        }

        private void PlatformSetupInstance(SoundEffectInstance inst)
        {
            inst.PlatformInitialize(this);
        }

        internal static void PlatformSetReverbSettings(ReverbSettings reverbSettings)
        {
        }

        private void PlatformDispose(bool disposing)
        {
            if (NativeData != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(NativeData);
                NativeData = IntPtr.Zero;
            }
            if (NativeFormat != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(NativeFormat);
                NativeFormat = IntPtr.Zero;
            }
        }

        internal static void PlatformInitialize()
        {
        }

        internal static void PlatformShutdown()
        {
        }
    }
}
