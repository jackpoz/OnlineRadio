using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.Collections.ObjectModel;
using System.Net.Http;
using OnlineRadio.Core.StreamHandlers;

namespace OnlineRadio.Core
{
    public class Radio : IDisposable
    {
        static readonly ReadOnlyCollection<string> metadataSongPatterns = new ReadOnlyCollection<string>(new string[]
        {
            @"StreamTitle='(?<title>[^~]+?) - (?<artist>[^~;]+?)?';",
            @"StreamTitle='(?<title>.+?)~(?<artist>.+?)~"
        });

        public string Url
        {
            get;
            private set;
        }
        public bool ArtistTitleOrderInverted
        {
            get;
            private set;
        }
        public bool Running
        {
            get
            {
                return _running;
            }
            set
            {
                _running = value;
                if (!_running && runningTask != null)
                    runningTask.Wait();
            }
        }
        bool _running;
        Task runningTask;
        public string Metadata
        {
            get
            {
                return _metadata;
            }
            private set
            {
                OnMetadataChanged?.Invoke(this, new MetadataEventArgs(_metadata, value));
                _metadata = value;
            }
        }
        string _metadata;
        public event EventHandler<MetadataEventArgs> OnMetadataChanged;

        public SongInfo CurrentSong
        {
            get
            {
                return _currentSong;
            }
            private set
            {
                OnCurrentSongChanged?.Invoke(this, new CurrentSongEventArgs(_currentSong, value));
                _currentSong = value;
            }
        }
        SongInfo _currentSong;
        public event EventHandler<CurrentSongEventArgs> OnCurrentSongChanged;

        public event EventHandler<StreamStartEventArgs> OnStreamStart;

        public event EventHandler<StreamUpdateEventArgs> OnStreamUpdate;

        public event EventHandler<StreamOverEventArgs> OnStreamOver;

        public float Volume
        {
            get
            {
                return _volume;
            }
            set
            {
                OnVolumeUpdate?.Invoke(this, new VolumeUpdateEventArgs(value));
                _volume = value;
            }
        }
        float _volume = 0.25f;
        public event EventHandler<VolumeUpdateEventArgs> OnVolumeUpdate;

        public event EventHandler<PluginEventArgs> OnPluginsLoaded;

        public static event EventHandler<MessageLogEventArgs> OnMessageLogged;

        PluginManager pluginManager;
        HttpClient httpClient;

        public Radio(string Url, bool ArtistTitleOrderInverted)
        {
            this.Url = Url;
            this.ArtistTitleOrderInverted = ArtistTitleOrderInverted;
            OnMetadataChanged += UpdateCurrentSong;
            pluginManager = new PluginManager();
            this.httpClient = new HttpClient
            {
                Timeout = new TimeSpan(0, 0, 10)
            };
        }

        public void Start(string pluginsPath = null)
        {
            if (string.IsNullOrEmpty(Url))
            {
                Radio.Log("The specified Url is empty.", this);
                return;
            }

            if (pluginsPath == null)
                pluginsPath = Directory.GetCurrentDirectory() + "\\plugins";
            var plugins = pluginManager.LoadPlugins(pluginsPath);
            OnPluginsLoaded?.Invoke(this, new PluginEventArgs(plugins));
            OnCurrentSongChanged += pluginManager.OnCurrentSongChanged;
            OnStreamStart += pluginManager.OnStreamStart;
            OnStreamUpdate += pluginManager.OnStreamUpdate;
            OnStreamOver += pluginManager.OnStreamOver;
            OnVolumeUpdate += pluginManager.OnVolumeUpdate;
            Running = true;
            runningTask = Task.Run(GetHttpStreamAsync);
        }

        async Task GetHttpStreamAsync()
        {
            do
            {
                try
                {
                    using var streamHandler = await BaseStreamHandler.GetStreamHandler(Url, httpClient);
                    await streamHandler.StartAsync();
                    {
                        //get the position of metadata
                        int metaInt = streamHandler.GetIceCastMetaInterval();

                        using MemoryStream metadataData = new MemoryStream();
                        byte[] buffer = null;
                        int metadataLength = 0;
                        int streamPosition = 0;
                        int bufferPosition = 0;
                        int readBytes = 0;

                        OnStreamStart?.Invoke(this, new StreamStartEventArgs(streamHandler.GetCodec()));

                        while (Running)
                        {                            
                            if (bufferPosition >= readBytes)
                            {
                                (readBytes, buffer) = await streamHandler.ReadAsync();
                                bufferPosition = 0;
                            }
                            if (readBytes <= 0)
                            {
                                Radio.Log("Stream over", this);
                                break;
                            }

                            if (metadataLength == 0)
                            {
                                if (metaInt == 0 || streamPosition + readBytes - bufferPosition <= metaInt)
                                {
                                    streamPosition += readBytes - bufferPosition;
                                    ProcessStreamData(buffer, ref bufferPosition, readBytes - bufferPosition);
                                    continue;
                                }

                                ProcessStreamData(buffer, ref bufferPosition, metaInt - streamPosition);
                                metadataLength = Convert.ToInt32(buffer[bufferPosition++]) * 16;
                                //check if there's any metadata, otherwise skip to next block
                                if (metadataLength == 0)
                                {
                                    streamPosition = Math.Min(readBytes - bufferPosition, metaInt);
                                    ProcessStreamData(buffer, ref bufferPosition, streamPosition);
                                    continue;
                                }
                            }

                            //get the metadata and reset the position
                            while (bufferPosition < readBytes)
                            {
                                metadataData.WriteByte(buffer[bufferPosition++]);
                                metadataLength--;
                                if (metadataLength == 0)
                                {
                                    var metadataBuffer = metadataData.ToArray();
                                    Metadata = Encoding.UTF8.GetString(metadataBuffer);
                                    metadataData.SetLength(0);
                                    streamPosition = Math.Min(readBytes - bufferPosition, metaInt);
                                    ProcessStreamData(buffer, ref bufferPosition, streamPosition);
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (IOException ex)
                {
                    Radio.Log(string.Format("Handled IOException, reconnecting. Details:\n{0}\n{1}", ex.Message, ex.StackTrace), this);
                    OnStreamOver?.Invoke(this, new StreamOverEventArgs());
                }
                catch (SocketException ex)
                {
                    Radio.Log(string.Format("Handled SocketException, reconnecting. Details:\n{0}\n{1}", ex.Message, ex.StackTrace), this);
                    OnStreamOver?.Invoke(this, new StreamOverEventArgs());
                }
                catch (WebException ex)
                {
                    Radio.Log(string.Format("Handled WebException, reconnecting. Details:\n{0}\n{1}", ex.Message, ex.StackTrace), this);
                    OnStreamOver?.Invoke(this, new StreamOverEventArgs());
                }
                catch(Exception ex)
                {
                    Radio.Log(string.Format("Handled Exception, reconnecting. Details:\n{0}\n{1}", ex.Message, ex.StackTrace), this);
                    OnStreamOver?.Invoke(this, new StreamOverEventArgs());
                }
            } while (Running);
        }

        void UpdateCurrentSong(object sender, MetadataEventArgs args)
        {
            foreach (var metadataSongPattern in metadataSongPatterns)
            {
                Match match = Regex.Match(args.NewMetadata, metadataSongPattern);
                if (match.Success)
                {
                    string artist = match.Groups["artist"].Value.Trim();
                    string title = match.Groups["title"].Value.Trim();
                    if (ArtistTitleOrderInverted)
                        CurrentSong = new SongInfo(title, artist);
                    else
                        CurrentSong = new SongInfo(artist, title);                
                    return;
                }
            }
        }

        void ProcessStreamData(byte[] buffer, ref int offset, int length)
        {
            if (length < 1)
                return;
            if (OnStreamUpdate != null)
            {
                byte[] data = new byte[length];
                Buffer.BlockCopy(buffer, offset, data, 0, length);
                OnStreamUpdate(this, new StreamUpdateEventArgs(data));
            }
            offset += length;
        }

        public static void Log(string Log, object sender)
        {
            OnMessageLogged?.Invoke(sender, new MessageLogEventArgs(Log));
        }

        IntPtr _disposed = IntPtr.Zero;
        public void Dispose()
        {
            // Thread-safe single disposal
            if (Interlocked.Exchange(ref _disposed, (IntPtr)1) != IntPtr.Zero)
                return;

            Running = false;
            OnCurrentSongChanged -= pluginManager.OnCurrentSongChanged;
            OnStreamStart -= pluginManager.OnStreamStart;
            OnStreamUpdate -= pluginManager.OnStreamUpdate;
            OnStreamOver -= pluginManager.OnStreamOver;
            pluginManager.Dispose();
            pluginManager = null;
            OnMessageLogged = null;
            httpClient.Dispose();
            httpClient = null;
        }

        public void Stop()
        {
            Dispose();
        }
    }

    public class SongInfo
    {
        public string Artist
        {
            get;
            private set;
        }
        public string Title
        {
            get;
            private set;
        }

        public SongInfo(string Artist, string Title)
        {
            this.Artist = Artist;
            this.Title = Title;
        }
    }

    public class MetadataEventArgs : EventArgs
    {
        public string OldMetadata { get; private set; }
        public string NewMetadata { get; private set; }

        public MetadataEventArgs(string OldMetadata, string NewMetadata)
        {
            this.OldMetadata = OldMetadata;
            this.NewMetadata = NewMetadata;
        }
    }

    public class CurrentSongEventArgs : EventArgs
    {
        public SongInfo OldSong { get; private set; }
        public SongInfo NewSong { get; private set; }

        public CurrentSongEventArgs(SongInfo OldSong, SongInfo NewSong)
        {
            this.OldSong = OldSong;
            this.NewSong = NewSong;
        }
    }

    public class StreamStartEventArgs : EventArgs
    {
        public string Codec { get; private set; }

        public StreamStartEventArgs(string codec)
        {
            this.Codec = codec;
        }
    }

    public class StreamUpdateEventArgs : EventArgs
    {
        public byte[] Data { get; private set; }

        public StreamUpdateEventArgs(byte[] Data)
        {
            this.Data = Data;
        }
    }

    public class StreamOverEventArgs : EventArgs
    {
        public StreamOverEventArgs()
        {
        }
    }

    public class VolumeUpdateEventArgs : EventArgs
    {
        public float Volume { get; private set; }

        public VolumeUpdateEventArgs(float Volume)
        {
            this.Volume = Volume;
        }
    }

    public class MessageLogEventArgs : EventArgs
    {
        public string Message { get; private set; }

        public MessageLogEventArgs(string Message)
        {
            this.Message = Message;
        }
    }

    public class PluginEventArgs : EventArgs
    {
        public List<IPlugin> Plugins { get; private set; }

        public PluginEventArgs(List<IPlugin> Plugins)
        {
            this.Plugins = Plugins;
        }
    }
}