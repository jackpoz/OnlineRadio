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

namespace OnlineRadio.Plugins.SongPreferences
{
    /// <summary>
    /// Interaction logic for FavouriteSongButton.xaml
    /// </summary>
    public partial class FavouriteSongButton : UserControl
    {
        public bool IsCurrentSongFavourite
        {
            get
            {
                return _isCurrentSongFavourite;
            }
            set
            {
                if (value)
                    FavouriteSongBtn.Content = "\uE195";
                else
                    FavouriteSongBtn.Content = "\uE113";
                _isCurrentSongFavourite = value;
            }
        }
        bool _isCurrentSongFavourite;


        public FavouriteSongButton()
        {
            InitializeComponent();
        }

        private void FavouriteSongBtn_Click(object sender, RoutedEventArgs e)
        {
            IsCurrentSongFavourite = !IsCurrentSongFavourite;
        }
    }
}
