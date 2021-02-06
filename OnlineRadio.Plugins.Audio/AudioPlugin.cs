using System;
using OnlineRadio.Core;
using System.IO;
using NAudio.Wave;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace OnlineRadio.Plugins.Audio
{
    public sealed class AudioPlugin : IPlugin, IDisposable
    {
        public string Name
        {
            get { return "AudioPlugin"; }
        }

        bool IsPlaying
        {
            get;
            set;
        }
        Task playTask;

        string Codec
        {
            get;
            set;
        }

        float Volume
        {
            get
            {
                return _volume;
            }
            set
            {
                _volume = value;
                if (waveOut != null)
                    waveOut.Volume = Math.Clamp(value, 0f, 1f);
            }
        }
        float _volume;

        public AudioPlugin()
        {
            stream = new SlidingStream();
        }

        void IPlugin.OnCurrentSongChanged(object sender, CurrentSongEventArgs args)
        {
            // Do nothing
        }

        void IPlugin.OnStreamStart(object sender, StreamStartEventArgs args)
        {
            Codec = args.Codec;
            _volume = (sender as Radio)?.Volume ?? 0f;
        }

        void IPlugin.OnStreamUpdate(object sender, StreamUpdateEventArgs args)
        {
            stream.Write(args.Data, 0, args.Data.Length);
            if (!IsPlaying)
                StartPlay();
        }

        void IPlugin.OnStreamOver(object sender, StreamOverEventArgs args)
        {
            IsPlaying = false;
            playTask?.Wait();
        }

        void IPlugin.OnVolumeUpdate(object sender, VolumeUpdateEventArgs args)
        {
            Volume = args.Volume;
        }

        void StartPlay()
        {
            IsPlaying = true;
            switch (Codec)
            {
                case "mp3":
                    playTask = Task.Run(PlayMP3Async);
                    break;
                case "mp4a":
                    playTask = Task.Run(PlayMP4AAsync);
                    break;
                default:
                    throw new NotSupportedException($"Codec {Codec} is not supported by {nameof(AudioPlugin)}");
            }
        }

        #region NAudio
        readonly SlidingStream stream;
        IWavePlayer waveOut;
        IMp3FrameDecompressor decompressor;
        BufferedWaveProvider bufferedWaveProvider;
        StreamMediaFoundationReader mediaReader;

        private async Task PlayMP3Async()
        {
            byte[] buffer = new byte[16384 * 4]; // needs to be big enough to hold a decompressed frame

            do
            {
                try
                {
                    //WaveBuffer getting full, taking a break
                    if (bufferedWaveProvider != null && bufferedWaveProvider.BufferLength - bufferedWaveProvider.BufferedBytes < bufferedWaveProvider.WaveFormat.AverageBytesPerSecond / 2)
                    {
                        await Task.Delay(500).ConfigureAwait(false);
                    }
                    //StreamBuffer empty, taking a break
                    else if (stream.Length < 16384 / 4 )
                    {
                        await Task.Delay(500).ConfigureAwait(false);
                    }
                    else
                    {
                        Mp3Frame frame = Mp3Frame.LoadFromStream(stream);
                        if (frame == null)
                            continue;
                        if (decompressor == null)
                        {
                            WaveFormat waveFormat = new Mp3WaveFormat(frame.SampleRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2, frame.FrameLength, frame.BitRate);
                            decompressor = new AcmMp3FrameDecompressor(waveFormat);
                            bufferedWaveProvider = new BufferedWaveProvider(decompressor.OutputFormat);
                        }

                        try
                        {
                            int decompressed = decompressor.DecompressFrame(frame, buffer, 0);
                            if (decompressed > 0)
                                bufferedWaveProvider.AddSamples(buffer, 0, decompressed);
                        }
                        catch (NAudio.MmException)
                        {
                            // Just ignore the frame if a MmException occurs
                        }

                        if (waveOut == null)
                        {
                            waveOut = new WaveOutEvent();
                            VolumeWaveProvider16 volumeProvider = new VolumeWaveProvider16(bufferedWaveProvider);
                            volumeProvider.Volume = Volume;
                            waveOut.Init(volumeProvider);
                            waveOut.Play();
                        }
                    }
                }
                catch (EndOfStreamException)
                {
                    CleanUpAudio();
                }
                catch(Exception)
                {
                    CleanUpAudio();
                }
            } while (IsPlaying);

            CleanUpAudio();
        }

        private async Task PlayMP4AAsync()
        {
            do
            {
                try
                {
                    throw new NotImplementedException();
                }
                catch (EndOfStreamException)
                {
                    CleanUpAudio();
                }
                catch (Exception)
                {
                    CleanUpAudio();
                }
            } while (IsPlaying);

            CleanUpAudio();
        }

        private void CleanUpAudio()
        {
            if (waveOut != null)
            {
                waveOut.Stop();
                waveOut.Dispose();
                waveOut = null;
            }

            if (decompressor != null)
            {
                decompressor.Dispose();
                decompressor = null;
            }

            if (mediaReader != null)
            {
                mediaReader.Dispose();
                mediaReader = null;
            }

            bufferedWaveProvider = null;
            stream.Flush();
        }
        #endregion

        public void Dispose()
        {
            IsPlaying = false;
            if(playTask != null)
                playTask.Wait();
            stream.Dispose();
        }
    }

    class SlidingStream : Stream
    {
        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get { throw new NotImplementedException(); }
        }

        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        public override void Flush()
        {
            _length = 0;
            blocks = new ConcurrentQueue<byte[]>();
            currentBlock = null;
        }

        public override long Length
        {
            get { return _length; }
        }
        volatile int _length;

        public override long Position
        {
            get
            {
                return 0;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        ConcurrentQueue<byte[]> blocks;
        byte[] currentBlock;

        public SlidingStream()
        {
            blocks = new ConcurrentQueue<byte[]>();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_length < count)
                count = _length;

            if (blocks.Count == 0 && (currentBlock == null || currentBlock.Length == 0))
                return 0;

            int readCount = 0;
            if (currentBlock == null || currentBlock.Length == 0)
                if (!blocks.TryDequeue(out currentBlock))
                    throw new InvalidOperationException("Failed to dequeue from SlidingStream");

            while (readCount < count)
            {
                if (readCount + currentBlock.Length < count)
                {
                    Buffer.BlockCopy(currentBlock, 0, buffer, offset + readCount, currentBlock.Length);
                    readCount += currentBlock.Length;
                    if (!blocks.TryDequeue(out currentBlock))
                        throw new InvalidOperationException("Failed to dequeue from SlidingStream with half-read buffer");
                }
                else
                {
                    Buffer.BlockCopy(currentBlock, 0, buffer, offset + readCount, count - readCount);
                    //resize the queued buffer to store only the unread data
                    Buffer.BlockCopy(currentBlock, count - readCount, currentBlock, 0, currentBlock.Length - (count - readCount));
                    Array.Resize(ref currentBlock, currentBlock.Length - (count - readCount));
                    readCount = count;
                    break;
                }
            }

            Interlocked.Add(ref _length, -readCount);

            return readCount;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            byte[] bufferCopy = new byte[count];
            Buffer.BlockCopy(buffer, offset, bufferCopy, 0, count);
            blocks.Enqueue(bufferCopy);
            Interlocked.Add(ref _length, count);
        }
    }
}
