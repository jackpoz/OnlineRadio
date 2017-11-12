using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineRadio.Core;
using System.IO;
using NAudio.Wave;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace OnlineRadio.Plugins.Audio
{
    public class AudioPlugin : IPlugin, IDisposable
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

        public AudioPlugin()
        {
            stream = new SlidingStream();
        }

        public void OnCurrentSongChanged(object sender, CurrentSongEventArgs args)
        {}

        public void OnStreamUpdate(object sender, StreamUpdateEventArgs args)
        {
            stream.Write(args.Data, 0, args.Data.Length);
            if (!IsPlaying)
                StartPlay();
        }

        public void OnStreamOver(object sender, StreamOverEventArgs args)
        {
            IsPlaying = false;
            playTask?.Wait();
            StartPlay();
        }

        void StartPlay()
        {
            IsPlaying = true;
            playTask = Task.Run(DecompressFrames);
        }

        #region NAudio
        SlidingStream stream;
        IWavePlayer waveOut;
        IMp3FrameDecompressor decompressor;
        BufferedWaveProvider bufferedWaveProvider;

        private async Task DecompressFrames()
        {
            byte[] buffer = new byte[16384 * 4]; // needs to be big enough to hold a decompressed frame

            do
            {
                try
                {
                    //WaveBuffer getting full, taking a break
                    if (bufferedWaveProvider != null && bufferedWaveProvider.BufferLength - bufferedWaveProvider.BufferedBytes < bufferedWaveProvider.WaveFormat.AverageBytesPerSecond / 2)
                    {
                        await Task.Delay(500);
                    }
                    //StreamBuffer empty, taking a break
                    else if (stream.Length < 16384 * 2)
                    {
                        await Task.Delay(500);
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
                            bufferedWaveProvider.BufferDuration = TimeSpan.FromSeconds(5); // allow us to get well ahead of ourselves
                        }

                        try
                        {
                            int decompressed = decompressor.DecompressFrame(frame, buffer, 0);
                            if (decompressed > 0)
                                bufferedWaveProvider.AddSamples(buffer, 0, decompressed);
                        }
                        catch (NAudio.MmException)
                        { }

                        if (waveOut == null)
                        {
                            waveOut = new WaveOut();
                            VolumeWaveProvider16 volumeProvider = new VolumeWaveProvider16(bufferedWaveProvider);
                            volumeProvider.Volume = 0.5f;
                            waveOut.Init(volumeProvider);
                            waveOut.Play();
                        }
                    }
                }
                catch (EndOfStreamException)
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
            get { throw new NotImplementedException(); }
        }

        public override bool CanSeek
        {
            get { throw new NotImplementedException(); }
        }

        public override bool CanWrite
        {
            get { throw new NotImplementedException(); }
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
                    continue;
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
