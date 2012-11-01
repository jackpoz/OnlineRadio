using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineRadio.Core;

namespace OnlineRadio.Plugins.Audio
{
    public class AudioPlugin : IPlugin
    {
        string IPlugin.Name
        {
            get { return "AudioPlugin"; }
        }

        void IPlugin.OnCurrentSongChanged(object sender, CurrentSongEventArgs args)
        {}

        void IPlugin.OnStreamUpdate(object sender, StreamUpdateEventArgs args)
        {
            throw new NotImplementedException();
        }
    }
}
