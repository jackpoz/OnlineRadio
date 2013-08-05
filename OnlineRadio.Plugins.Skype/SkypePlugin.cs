using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineRadio.Core;
using SKYPE4COMLib;

namespace OnlineRadio.Plugins.Skype
{
    public class SkypePlugin : IPlugin, IDisposable
    {
        #region SkypeSetMusic
        string initialMood;

        public SkypePlugin()
        {
            SKYPE4COMLib.Skype skype = new SKYPE4COMLib.Skype();

            if (!skype.Client.IsRunning)
                return;

            initialMood = skype.CurrentUserProfile.RichMoodText ?? String.Empty;
        }

        private void SetSong(string p1, string p2)
        {
            SKYPE4COMLib.Skype skype = new SKYPE4COMLib.Skype();

            if (!skype.Client.IsRunning)
                return;

            skype.CurrentUserProfile.RichMoodText = "<SS type=\"music\">(music)</SS> " + p1 + " - " + p2;
        }

        private void RemoveSong()
        {
            SKYPE4COMLib.Skype skype = new SKYPE4COMLib.Skype();

            if (!skype.Client.IsRunning)
                return;

            skype.CurrentUserProfile.RichMoodText = initialMood;
        }
        #endregion

        public void Dispose()
        {
            RemoveSong();
        }

        public string Name
        {
            get { return "SkypePlugin"; }
        }

        public void OnCurrentSongChanged(object sender, CurrentSongEventArgs args)
        {
            SetSong(args.NewSong.Title, args.NewSong.Artist);
        }

        public void OnStreamUpdate(object sender, StreamUpdateEventArgs args)
        { }
    }
}
