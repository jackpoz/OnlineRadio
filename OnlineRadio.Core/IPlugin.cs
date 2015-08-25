using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineRadio.Core
{
    public interface IPlugin
    {
        string Name
        {
            get;
        }

        void OnCurrentSongChanged(object sender, CurrentSongEventArgs args);

        void OnStreamUpdate(object sender, StreamUpdateEventArgs args);

        void OnStreamOver(object sender, StreamOverEventArgs args);
    }

    public abstract class BasePlugin : IPlugin
    {
        public string Name
        {
            get 
            { 
                return "BasePlugin";
            }
        }

        public void OnCurrentSongChanged(object sender, CurrentSongEventArgs args)
        {}

        public void OnStreamUpdate(object sender, StreamUpdateEventArgs args)
        { }

        public void OnStreamOver(object sender, StreamOverEventArgs args)
        { }
    }
}
