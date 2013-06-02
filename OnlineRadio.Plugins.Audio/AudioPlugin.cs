using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineRadio.Core;
using System.IO;
using NAudio.Wave;
using System.Threading;
using System.Threading.Tasks;

namespace OnlineRadio.Plugins.Audio
{
    public class AudioPlugin : IPlugin, IDisposable
    {
        string IPlugin.Name
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

        void IPlugin.OnCurrentSongChanged(object sender, CurrentSongEventArgs args)
        {}

        void IPlugin.OnStreamUpdate(object sender, StreamUpdateEventArgs args)
        {
            stream.Write(args.Data, 0, args.Data.Length);
            if (!IsPlaying)
                StartPlay();
        }

        void StartPlay()
        {
            IsPlaying = true;
            playTask = Task.Factory.StartNew(() => DecompressFrames());
        }

        #region NAudio
        SlidingStream stream;

        private void DecompressFrames()
        {
            IMp3FrameDecompressor decompressor = null;
            IWavePlayer waveOut = null;
            try
            {
                BufferedWaveProvider bufferedWaveProvider = null;
                VolumeWaveProvider16 volumeProvider = null;
                byte[] buffer = new byte[16384 * 4]; // needs to be big enough to hold a decompressed frame
                bool firstLoop = true;

                do
                {
                    //WaveBuffer getting full, taking a break
                    if (bufferedWaveProvider != null && bufferedWaveProvider.BufferLength - bufferedWaveProvider.BufferedBytes < bufferedWaveProvider.WaveFormat.AverageBytesPerSecond / 4)
                    {
                        Thread.Sleep(500);
                    }
                    //StreamBuffer empty, taking a break
                    else if (stream.Length < 16384 * 2)
                    {
                        Thread.Sleep(500);
                    }
                    else
                    {
                        Mp3Frame frame = null;
                        try
                        {
                            frame = Mp3Frame.LoadFromStream(stream);
                        }
                        catch (EndOfStreamException)
                        {
                            break;
                        }
                        if (frame == null)
                            continue;
                        if (decompressor == null)
                        {
                            WaveFormat waveFormat = new Mp3WaveFormat(frame.SampleRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2, frame.FrameLength, frame.BitRate);
                            decompressor = new AcmMp3FrameDecompressor(waveFormat);
                            bufferedWaveProvider = new BufferedWaveProvider(decompressor.OutputFormat);
                            bufferedWaveProvider.BufferDuration = TimeSpan.FromSeconds(5); // allow us to get well ahead of ourselves
                        }
                        int decompressed = decompressor.DecompressFrame(frame, buffer, 0);
                        if (decompressed > 0)
                            bufferedWaveProvider.AddSamples(buffer, 0, decompressed);

                        if (firstLoop)
                        {
                            firstLoop = false;
                            waveOut = CreateWaveOut();
                            volumeProvider = new VolumeWaveProvider16(bufferedWaveProvider);
                            volumeProvider.Volume = 0.5f;
                            waveOut.Init(volumeProvider);
                            waveOut.Play();
                        }
                    }

                } while (IsPlaying);
            }
            finally
            {
                if (waveOut != null)
                {
                    waveOut.Stop();
                    waveOut.Dispose();
                }
                if (decompressor != null)
                {
                    decompressor.Dispose();
                }
            }
        }

        private IWavePlayer CreateWaveOut()
        {
            return new WaveOut();
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
            throw new NotImplementedException();
        }

        public override long Length
        {
            get { return _length; }
        }
        int _length;

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

        Queue<byte[]> blocks;
        byte[] currentBlock;

        public SlidingStream()
        {
            blocks = new Queue<byte[]>();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_length < count)
                count = _length;

            if (blocks.Count == 0)
                return 0;

            int readCount = 0;
            if (currentBlock == null || currentBlock.Length == 0)
                currentBlock = blocks.Dequeue();

            while (readCount < count)
            {
                if (readCount + currentBlock.Length < count)
                {
                    Buffer.BlockCopy(currentBlock, 0, buffer, offset + readCount, currentBlock.Length);
                    readCount += currentBlock.Length;
                    currentBlock = blocks.Dequeue();
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

            _length -= readCount;

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
            _length += count;
        }
    }
}
