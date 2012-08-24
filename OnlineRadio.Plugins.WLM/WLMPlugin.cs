using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineRadio.Core;

namespace OnlineRadio.Plugins.WLM
{
    public class WLMPlugin : IPlugin
    {
        string IPlugin.Name
        {
            get { return "WLMPlugin"; }
        }

        void IPlugin.OnCurrentSongChanged(object sender, CurrentSongEventArgs args)
        {
            throw new NotImplementedException();
        }
    }
}
