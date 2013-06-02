using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineRadio.Core;

namespace OnlineRadio.Plugins.Lyrics
{
    public class LyricsPlugin : IPlugin
    {
        public string Name
        {
            get { return "LyricsPlugin"; }
        }

        public void OnCurrentSongChanged(object sender, CurrentSongEventArgs args)
        {
        }

        public void OnStreamUpdate(object sender, StreamUpdateEventArgs args)
        {
        }
    }
}
