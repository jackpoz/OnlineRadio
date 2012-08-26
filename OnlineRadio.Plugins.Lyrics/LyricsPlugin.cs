using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineRadio.Core;

namespace OnlineRadio.Plugins.Lyrics
{
    public class LyricsPlugin : IPlugin
    {
        string IPlugin.Name
        {
            get { return "LyricsPlugin"; }
        }

        void IPlugin.OnCurrentSongChanged(object sender, CurrentSongEventArgs args)
        {
            throw new NotImplementedException();
        }
    }
}
