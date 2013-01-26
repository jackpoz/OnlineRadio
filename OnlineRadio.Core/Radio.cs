using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OnlineRadio.Core
{
    public class Radio : IDisposable
    {
        public string Url
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
                if (OnMetadataChanged != null)
                    OnMetadataChanged(this, new MetadataEventArgs(_metadata, value));
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
                if (OnCurrentSongChanged != null)
                    OnCurrentSongChanged(this, new CurrentSongEventArgs(_currentSong, value));
                _currentSong = value;
            }
        }
        SongInfo _currentSong;
        public event EventHandler<CurrentSongEventArgs> OnCurrentSongChanged;

        public event EventHandler<StreamUpdateEventArgs> OnStreamUpdate;

        PluginManager pluginManager;

        public Radio(string Url)
        {
            this.Url = Url;
            OnMetadataChanged += UpdateCurrentSong;
            pluginManager = new PluginManager();
        }

        public void Start(string pluginsPath = null)
        {
            if (pluginsPath == null)
                pluginsPath = Directory.GetCurrentDirectory() + "\\plugins";
            pluginManager.LoadPlugins(pluginsPath);
            OnCurrentSongChanged += pluginManager.OnCurrentSongChanged;
            OnStreamUpdate += pluginManager.OnStreamUpdate;
            Running = true;
            runningTask = Task.Run(() => GetHttpStream());
        }

        void GetHttpStream()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.Headers.Add("icy-metadata", "1");
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                //get the position of metadata
                int metaInt = Convert.ToInt32(response.GetResponseHeader("icy-metaint"));
                using (Stream socketStream = response.GetResponseStream())
                {
                    byte[] buffer = new byte[16384];
                    int metadataLength = 0;
                    int streamPosition = 0;
                    int index = -1;
                    StringBuilder metadataSb = new StringBuilder();
                    int bytes = 0;

                    while (Running)
                    {
                        bytes = socketStream.Read(buffer, 0, buffer.Length);
                        if (bytes < 0)
                            throw new Exception("Nothing read");

                        if (metadataLength == 0)
                        {
                            if (streamPosition + bytes <= metaInt)
                            {
                                streamPosition += bytes;
                                ProcessStreamData(buffer, 0, bytes);
                                continue;
                            }

                            index = metaInt - streamPosition;
                            ProcessStreamData(buffer, 0, index);
                            metadataLength = Convert.ToInt32(buffer[index]) * 16;
                            //check if there's any metadata, otherwise skip to next block
                            if (metadataLength == 0)
                            {
                                streamPosition = bytes - index - 1;
                                ProcessStreamData(buffer, index + 1, streamPosition);
                                continue;
                            }
                            index++;
                        }

                        //get the metadata and reset the position
                        for (; index < bytes; index++)
                        {
                            metadataSb.Append(Convert.ToChar(buffer[index]));
                            metadataLength--;
                            if (metadataLength == 0)
                            {
                                Metadata = metadataSb.ToString();
                                metadataSb.Clear();
                                streamPosition = bytes - index - 1;
                                ProcessStreamData(buffer, index + 1, streamPosition);
                                break;
                            }
                        }
                    }
                }
            }
        }

        void UpdateCurrentSong(object sender, MetadataEventArgs args)
        {
            const string metadataSongPattern = @"StreamTitle='(?<artist>.+) - (?<title>.+)';";
            Match match = Regex.Match(args.NewMetadata, metadataSongPattern);
            if (!match.Success)
                CurrentSong = null;
            else
                CurrentSong = new SongInfo(match.Groups["artist"].Value, match.Groups["title"].Value);
        }

        void ProcessStreamData(byte[] buffer, int offset, int length)
        {
            if (length < 1)
                return;
            if (OnStreamUpdate != null)
            {
                byte[] data = new byte[length];
                Buffer.BlockCopy(buffer, offset, data, 0, length);
                OnStreamUpdate(this, new StreamUpdateEventArgs(data));
            }
        }

        public void Dispose()
        {
            Running = false;
            pluginManager.Dispose();
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
}