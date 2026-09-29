// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Audio;
using DS = Rxdk.Audio;

namespace Microsoft.Xna.Framework.Media
{
    /// <summary>
    /// Xbox songs. The content pipeline leaves a song as the original .wma beside its .xnb, and
    /// the SDK's WMA decoder reads that file straight off the disc. Decoded PCM goes through a ring
    /// of packets into a DirectSound stream; the run loop refills the ring every frame through
    /// <see cref="Update"/>, which is also where a finished song is reported. Reporting it from
    /// inside Play would recurse, because MediaPlayer answers a finished song by playing the next.
    ///
    /// A song whose file will not decode stays silent: it reports its position as zero and never
    /// finishes, which is how every song behaved before the decoder was wired up.
    /// </summary>
    public sealed partial class Song : IEquatable<Song>, IDisposable
    {
        const int PacketCount = 8;
        const int PacketBytes = 16384;
        const int PacketSize = 24;
        const int LookaheadBytes = 65536;
        const int StreamDescSize = 24;
        const int FormatBytes = 64;
        const int StatusPending = unchecked((int)0x8000000A);
        const int PauseResume = 0;
        const int PausePause = 1;

        static Song _active;

        FinishedPlayingHandler _finished;
        float _volume = 1.0f;
        TimeSpan _position;

        IntPtr _decoder;
        IntPtr _stream;
        IntPtr _format;
        // Per packet: an XMEDIAPACKET, then the status and completed-size DWORDs it points at.
        IntPtr _packets;
        IntPtr _buffers;
        bool[] _submitted = new bool[PacketCount];
        bool _endOfFile;
        long _playedBytes;
        int _bytesPerSecond;

        private void PlatformInitialize(string fileName)
        {
        }

        private void PlatformDispose(bool disposing)
        {
            Close();
            _finished = null;
        }

        internal void OnFinishedPlaying(object sender, EventArgs args)
        {
            if (_finished != null)
                _finished(sender, args);
        }

        internal void SetEventHandler(FinishedPlayingHandler handler)
        {
            _finished = handler;
        }

        internal void Play(TimeSpan? startPosition)
        {
            if (_active != null)
                _active.Close();
            _position = TimeSpan.Zero;
            if (!Open())
                return;
            _active = this;
            Fill();
            _playCount++;
        }

        private void PlatformPlay()
        {
            Play(null);
        }

        internal void Resume()
        {
            if (_stream != IntPtr.Zero)
                DS.IDirectSoundStream_Pause(_stream, PauseResume);
        }

        private void PlatformResume()
        {
            Resume();
        }

        internal void Pause()
        {
            if (_stream != IntPtr.Zero)
                DS.IDirectSoundStream_Pause(_stream, PausePause);
        }

        internal void Stop()
        {
            Close();
            _position = TimeSpan.Zero;
        }

        internal float Volume
        {
            get { return _volume; }
            set
            {
                _volume = value;
                if (_stream != IntPtr.Zero)
                    DS.IDirectSoundStream_SetVolume(_stream, XboxAudio.ToMillibels(value));
            }
        }

        internal TimeSpan Position
        {
            get
            {
                if (_bytesPerSecond > 0)
                    return TimeSpan.FromSeconds((double)_playedBytes / _bytesPerSecond);
                return _position;
            }
            set { _position = value; }
        }

        /// <summary>
        /// Called once a frame by the run loop.
        /// </summary>
        internal static void Update()
        {
            var song = _active;
            if (song == null)
                return;
            song.Fill();
            if (song._endOfFile && !song.AnyPending())
            {
                song.Close();
                song.OnFinishedPlaying(song, EventArgs.Empty);
            }
        }

        unsafe bool Open()
        {
            var device = XboxAudio.Device;
            if (device == IntPtr.Zero || string.IsNullOrEmpty(FilePath))
                return false;
            string path = FilePath.Replace('/', '\\');
            if (!Path.IsPathRooted(path))
                path = Path.Combine(TitleContainer.Location, path);

            _format = AllocZeroed(FormatBytes);
            IntPtr decoder;
            if (DS.WmaCreateDecoder(path, IntPtr.Zero, false, LookaheadBytes, PacketCount, 0, _format, (IntPtr)(&decoder)) < 0
                || decoder == IntPtr.Zero)
            {
                Close();
                return false;
            }
            _decoder = decoder;
            _bytesPerSecond = ((int*)_format)[2];

            int* desc = stackalloc int[StreamDescSize / 4];
            desc[0] = 0;
            desc[1] = PacketCount;
            desc[2] = (int)_format;
            desc[3] = 0;
            desc[4] = 0;
            desc[5] = 0;
            IntPtr stream;
            if (DS.IDirectSound_CreateSoundStream(device, (IntPtr)desc, (IntPtr)(&stream), IntPtr.Zero) < 0
                || stream == IntPtr.Zero)
            {
                Close();
                return false;
            }
            _stream = stream;
            DS.IDirectSoundStream_SetVolume(_stream, XboxAudio.ToMillibels(_volume));

            _packets = AllocZeroed(PacketCount * (PacketSize + 8));
            _buffers = Marshal.AllocHGlobal(PacketCount * PacketBytes);
            for (int i = 0; i < PacketCount; i++)
                _submitted[i] = false;
            _endOfFile = false;
            _playedBytes = 0;
            return true;
        }

        static unsafe IntPtr AllocZeroed(int bytes)
        {
            var memory = Marshal.AllocHGlobal(bytes);
            int* words = (int*)memory;
            for (int i = 0; i < bytes / 4; i++)
                words[i] = 0;
            return memory;
        }

        unsafe int* Packet(int i)
        {
            return (int*)((byte*)_packets + i * (PacketSize + 8));
        }

        unsafe bool AnyPending()
        {
            for (int i = 0; i < PacketCount; i++)
                if (_submitted[i] && Packet(i)[6] == StatusPending)
                    return true;
            return false;
        }

        /// <summary>
        /// Decodes into every packet the stream has finished with and hands it back.
        /// </summary>
        unsafe void Fill()
        {
            if (_stream == IntPtr.Zero)
                return;
            for (int i = 0; i < PacketCount; i++)
            {
                int* packet = Packet(i);
                int* status = packet + 6;
                int* completed = packet + 7;
                if (_submitted[i])
                {
                    if (*status == StatusPending)
                        continue;
                    _submitted[i] = false;
                    _playedBytes += packet[1];
                }
                if (_endOfFile)
                    continue;

                packet[0] = (int)((byte*)_buffers + i * PacketBytes);
                packet[1] = PacketBytes;
                packet[2] = (int)completed;
                packet[3] = 0;
                packet[4] = 0;
                packet[5] = 0;
                *completed = 0;
                if (DS.XMediaObject_Process(_decoder, IntPtr.Zero, (IntPtr)packet) < 0 || *completed == 0)
                {
                    _endOfFile = true;
                    DS.XMediaObject_Discontinuity(_stream);
                    continue;
                }

                packet[1] = *completed;
                packet[2] = 0;
                packet[3] = (int)status;
                *status = StatusPending;
                if (DS.XMediaObject_Process(_stream, (IntPtr)packet, IntPtr.Zero) < 0)
                {
                    _endOfFile = true;
                    continue;
                }
                _submitted[i] = true;
            }
        }

        void Close()
        {
            if (_active == this)
                _active = null;
            if (_stream != IntPtr.Zero)
            {
                DS.XMediaObject_Flush(_stream);
                DS.XMediaObject_Release(_stream);
                _stream = IntPtr.Zero;
            }
            if (_decoder != IntPtr.Zero)
            {
                DS.XMediaObject_Release(_decoder);
                _decoder = IntPtr.Zero;
            }
            if (_packets != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_packets);
                _packets = IntPtr.Zero;
            }
            if (_buffers != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_buffers);
                _buffers = IntPtr.Zero;
            }
            if (_format != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_format);
                _format = IntPtr.Zero;
            }
            _bytesPerSecond = 0;
            _playedBytes = 0;
            for (int i = 0; i < PacketCount; i++)
                _submitted[i] = false;
        }

        private Album PlatformGetAlbum()
        {
            return null;
        }

        private Artist PlatformGetArtist()
        {
            return null;
        }

        private Genre PlatformGetGenre()
        {
            return null;
        }

        private bool PlatformIsProtected()
        {
            return false;
        }

        private bool PlatformIsRated()
        {
            return false;
        }

        private string PlatformGetName()
        {
            return _name;
        }

        private int PlatformGetPlayCount()
        {
            return _playCount;
        }

        private int PlatformGetRating()
        {
            return 0;
        }

        private int PlatformGetTrackNumber()
        {
            return 0;
        }
    }
}
