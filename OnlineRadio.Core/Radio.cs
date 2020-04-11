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

        public event EventHandler<StreamUpdateEventArgs> OnStreamUpdate;

        public event EventHandler<StreamOverEventArgs> OnStreamOver;

        public event EventHandler<PluginEventArgs> OnPluginsLoaded;

        public static event EventHandler<MessageLogEventArgs> OnMessageLogged;

        PluginManager pluginManager;

        public Radio(string Url, bool ArtistTitleOrderInverted)
        {
            this.Url = Url;
            this.ArtistTitleOrderInverted = ArtistTitleOrderInverted;
            OnMetadataChanged += UpdateCurrentSong;
            pluginManager = new PluginManager();
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
            OnStreamUpdate += pluginManager.OnStreamUpdate;
            OnStreamOver += pluginManager.OnStreamOver;
            Running = true;
            runningTask = Task.Run(GetHttpStreamAsync);
        }

        async Task GetHttpStreamAsync()
        {
            do
            {
                try
                {
                    string streamUrl = Url;

                    if (streamUrl.EndsWith(".m3u", StringComparison.InvariantCultureIgnoreCase))
                        streamUrl = await GetStreamUrlFromM3U(streamUrl);

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(streamUrl);
                    request.Headers.Add("icy-metadata", "1");
                    request.ReadWriteTimeout = 10 * 1000;
                    request.Timeout = 10 * 1000;
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        //get the position of metadata
                        int metaInt = 0;
                        if (!string.IsNullOrEmpty(response.GetResponseHeader("icy-metaint")))
                            metaInt = Convert.ToInt32(response.GetResponseHeader("icy-metaint"));

                        using (Stream socketStream = response.GetResponseStream())
                        using (MemoryStream metadataData = new MemoryStream())
                        {
                            byte[] buffer = new byte[16384];
                            int metadataLength = 0;
                            int streamPosition = 0;
                            int bufferPosition = 0;
                            int readBytes = 0;

                            while (Running)
                            {
                                if (bufferPosition >= readBytes)
                                {
                                    readBytes = await socketStream.ReadAsync(buffer, 0, buffer.Length);
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

        async Task<string> GetStreamUrlFromM3U(string streamUrl)
        {
            var client = new WebClient();
            var result = await client.DownloadStringTaskAsync(streamUrl);

            var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length == 0)
                throw new ArgumentException($"The specified m3u url points to an empty file");

            if (lines.First() == "#EXTM3U")
                throw new NotSupportedException("Extended m3u files are not supported");

            foreach(var line in lines)
            {
                // Skip comments
                if (line.StartsWith('#'))
                    continue;

                // The first line that is not a comment is the mp3 url
                return line;
            }

            throw new InvalidDataException("The specified m3u points to a document with no stream url");
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
            OnStreamUpdate -= pluginManager.OnStreamUpdate;
            OnStreamOver -= pluginManager.OnStreamOver;
            pluginManager.Dispose();
            pluginManager = null;
            OnMessageLogged = null;
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