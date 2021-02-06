using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Xml.Serialization;
using OnlineRadio.Core;
using static OnlineRadio.Plugins.SongPreferences.SongPreference;

namespace OnlineRadio.Plugins.SongPreferences
{
    public sealed class SongPreferencesPlugin : IButtonPlugin, IDisposable
    {
        const string preferencesPath = "songPreferences.xml";

        ConcurrentBag<SongPreference> songPreferences;

        public SongPreferencesPlugin()
        {
            if (!File.Exists(preferencesPath))
                songPreferences = new ConcurrentBag<SongPreference>();
            else using (StreamReader sr = new StreamReader(preferencesPath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<SongPreference>));
                    songPreferences = new ConcurrentBag<SongPreference>((List<SongPreference>)serializer.Deserialize(sr));
            }
        }

        string IPlugin.Name => "AudioPlugin";

        UserControl IButtonPlugin.Button
        {
            get
            {
                if (_button == null)
                {
                    _button = new FavouriteSongButton();
                    _button.OnSongPreferenceChanged += OnSongPreferenceChanged;
                }
                return _button;
            }
        }
        FavouriteSongButton _button;
        SongInfo _currentSong;

        void IPlugin.OnCurrentSongChanged(object sender, CurrentSongEventArgs args)
        {
            _currentSong = args.NewSong;
            _button.SetCurrentSongFavourite(songPreferences.Any(s => s.Artist == _currentSong.Artist && s.Title == _currentSong.Title && s.Preference == SongPreference.ESongPref.Favourite));
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

        void IPlugin.OnVolumeUpdate(object sender, VolumeUpdateEventArgs args)
        {
        }

        void OnSongPreferenceChanged(object sender, SongPreferenceEventArgs args)
        {
            var preference = songPreferences.FirstOrDefault(s => s.Artist == _currentSong.Artist && s.Title == _currentSong.Title);
            if (preference != null)
                preference.Preference = args.Preference;
            else
                songPreferences.Add(new SongPreference(_currentSong.Artist, _currentSong.Title, args.Preference));
        }

        public void Dispose()
        {
            using (StreamWriter sw = new StreamWriter(preferencesPath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<SongPreference>));
                serializer.Serialize(sw, songPreferences.ToList());
            }
        }
    }

    public class SongPreferenceEventArgs : EventArgs
    {
        public ESongPref Preference { get; private set; }

        public SongPreferenceEventArgs(ESongPref Preference)
        {
            this.Preference = Preference;
        }
    }
}
