using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineRadio.Plugins.SongPreferences
{
    public class SongPreference
    {
        public string Artist
        {
            get;
            set;
        }
        public string Title
        {
            get;
            set;
        }
        public ESongPref Preference
        {
            get;
            set;
        }

        public enum ESongPref
        {
            None = 0,
            Favourite = 1
        }
    }
}
