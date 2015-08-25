using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json.Linq;
using OnlineRadio.Core;

namespace OnlineRadio.Plugins.Lyrics
{
    /// <summary>
    /// Interaction logic for LyricsPlugin.xaml
    /// </summary>
    public partial class LyricsPlugin : UserControl, IVisualPlugin
    {
        const string lyricsUrl = "http://api.musixmatch.com/ws/1.1/matcher.lyrics.get?q_track={0}&q_artist={1}";
        readonly Encoding encode = System.Text.Encoding.GetEncoding("utf-8");

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

        string Lyrics
        {
            get
            {
                return _lyrics;
            }
            set
            {
                _lyrics = value;
                Dispatcher.InvokeAsync(() => LyricsText.Text = _lyrics);
            }
        }
        string _lyrics;

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

        public void OnCurrentSongChanged(object sender, CurrentSongEventArgs args)
        {
            Task.Run(() =>
            {
                try
                {
                    WebRequest request = GetRequest(string.Format(lyricsUrl, args.NewSong.Title, args.NewSong.Artist));
                    WebResponse response = request.GetResponse();
                    StreamReader responseStream = new StreamReader(response.GetResponseStream(), encode);
                    JObject json = JObject.Parse(responseStream.ReadToEnd());
                    if ((string)json["message"]["header"]["status_code"] == "401")
                        Lyrics = "Unathorized call to Lyrics API, please check \"apikey\" parameter in the plugin config file."
                                + "Current value: \"" + ApiKey + "\"";
                    else
                        Lyrics = (string)json["message"]["body"]["lyrics"]["lyrics_body"];
                }
                catch(ArgumentException)
                {
                    Lyrics = "Lyrics not found";
                }
            });
        }

        public void OnStreamUpdate(object sender, StreamUpdateEventArgs args)
        {
        }

        public void OnStreamOver(object sender, StreamOverEventArgs args)
        { }

        public LyricsPlugin()
        {
            InitializeComponent();
        }

        WebRequest GetRequest(string url)
        {
            url += "&apikey=" + ApiKey;
            WebRequest request = WebRequest.Create(url);
            return request;
        }
    }
}
