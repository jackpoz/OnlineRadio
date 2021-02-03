using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Xml.Serialization;
using OnlineRadio.Core;

namespace OnlineRadio.Plugins.SongPreferences
{
    public sealed class SongPreferencesPlugin : IButtonPlugin, IDisposable
    {
        const string preferencesPath = "songPreferences.xml";

        List<SongPreference> songPreferences;

        public SongPreferencesPlugin()
        {
            if (!File.Exists(preferencesPath))
                songPreferences = new List<SongPreference>();
            else using (StreamReader sr = new StreamReader(preferencesPath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<SongPreference>));
                    songPreferences = (List<SongPreference>)serializer.Deserialize(sr);
            }
        }

        string IPlugin.Name => "AudioPlugin";

        UserControl IButtonPlugin.Button
        {
            get
            {
                if (_button == null)
                    _button = new FavouriteSongButton();
                return _button;
            }
        }
        UserControl _button;

        void IPlugin.OnCurrentSongChanged(object sender, CurrentSongEventArgs args)
        {
        }

        void IPlugin.OnStreamOver(object sender, StreamOverEventArgs args)
        {
        }

        void IPlugin.OnStreamStart(object sender, StreamStartEventArgs args)
        {
        }

        void IPlugin.OnStreamUpdate(object sender, StreamUpdateEventArgs args)
        {
        }

        public void Dispose()
        {
            using (StreamWriter sw = new StreamWriter(preferencesPath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<SongPreference>));
                serializer.Serialize(sw, songPreferences);
            }
        }
    }
}
