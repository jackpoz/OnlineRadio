using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static OnlineRadio.Plugins.SongPreferences.SongPreference;

namespace OnlineRadio.Plugins.SongPreferences
{
    /// <summary>
    /// Interaction logic for FavouriteSongButton.xaml
    /// </summary>
    public partial class FavouriteSongButton : UserControl
    {
        bool IsCurrentSongFavourite
        {
            get
            {
                return _isCurrentSongFavourite;
            }
            set
            {
                if (value)
                    FavouriteSongBtn.Content = "\uE1CF";
                else
                    FavouriteSongBtn.Content = "\uE1CE";
                _isCurrentSongFavourite = value;
            }
        }
        bool _isCurrentSongFavourite;

        public EventHandler<SongPreferenceEventArgs> OnSongPreferenceChanged;

        public FavouriteSongButton()
        {
            InitializeComponent();
        }

        public void SetCurrentSongFavourite(bool favourite)
        {
            Dispatcher.InvokeAsync(() =>
            {
                IsCurrentSongFavourite = favourite;
            });
        }

        private void FavouriteSongBtn_Click(object sender, RoutedEventArgs e)
        {
            var args = new SongPreferenceEventArgs(!IsCurrentSongFavourite ? ESongPref.Favourite : ESongPref.None);
            OnSongPreferenceChanged?.Invoke(this, args);
            if (!args.CancelEvent)
                IsCurrentSongFavourite = !IsCurrentSongFavourite;
        }
    }
}
