using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Controls;
using OnlineRadio.Core;

namespace OnlineRadio.Plugins.Lyrics
{
    /// <summary>
    /// Interaction logic for LyricsPlugin.xaml
    /// </summary>
    public partial class LyricsPlugin : UserControl, IVisualPlugin
    {
        const string lyricsUrl = "http://api.musixmatch.com/ws/1.1/matcher.lyrics.get?q_track={0}&q_artist={1}";
        readonly HttpClient httpClient;

        UserControl IVisualPlugin.Control
        {
            get
            {
                return this;
            }
        }

        int IVisualPlugin.ColumnSpan
        {
            get
            {
                return 2;
            }
        }

        string IPlugin.Name
        {
            get { return "LyricsPlugin"; }
        }

        void SetLyrics(string lyrics)
        {
            Dispatcher.InvokeAsync(() => LyricsText.Text = lyrics);
        }

        string ApiKey
        {
            get
            {
                if (_apikey == null)
                {
                    Assembly currentAssembly = Assembly.GetCallingAssembly();
                    Configuration currentConfiguration = ConfigurationManager.OpenExeConfiguration(currentAssembly.Location);
                    _apikey = currentConfiguration.AppSettings.Settings["apikey"].Value;
                }
                return _apikey;
            }
        }
        string _apikey;

        void IPlugin.OnCurrentSongChanged(object sender, CurrentSongEventArgs args)
        {
            Task.Run(async () =>
            {
                try
                {
                    var response = await GetResponse(string.Format(lyricsUrl, args.NewSong.Title, args.NewSong.Artist)).ConfigureAwait(false);
                    JsonObject json = JsonNode.Parse(response).AsObject();
                    if ((int)json["message"]["header"]["status_code"] == 401)
                        SetLyrics("Unathorized call to Lyrics API, please check \"apikey\" parameter in the plugin config file."
                                + "Current value: \"" + ApiKey + "\"");
                    else
                        SetLyrics((string)json["message"]["body"]["lyrics"]["lyrics_body"]);
                }
                catch(ArgumentException)
                {
                    SetLyrics("Lyrics not found");
                }
            });
        }

        void IPlugin.OnStreamStart(object sender, StreamStartEventArgs args)
        {
            // Do nothing
        }

        void IPlugin.OnStreamUpdate(object sender, StreamUpdateEventArgs args)
        {
            // Do nothing
        }

        void IPlugin.OnStreamOver(object sender, StreamOverEventArgs args)
        {
            // Do nothing
        }

        void IPlugin.OnVolumeUpdate(object sender, VolumeUpdateEventArgs args)
        {
            // Do nothing
        }

        public LyricsPlugin()
        {
            httpClient = new HttpClient();
            InitializeComponent();
        }

        async Task<string> GetResponse(string url)
        {
            url += "&apikey=" + ApiKey;
            return await httpClient.GetStringAsync(url).ConfigureAwait(false);
        }
    }
}
