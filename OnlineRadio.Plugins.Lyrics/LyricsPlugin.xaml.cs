using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OnlineRadio.Core;

namespace OnlineRadio.Plugins.Lyrics
{
    /// <summary>
    /// Interaction logic for LyricsPlugin.xaml
    /// </summary>
    public partial class LyricsPlugin : UserControl, IVisualPlugin
    {
        UserControl IVisualPlugin.Control
        {
            get
            {
                return this;
            }
        }

        string IPlugin.Name
        {
            get { return "LyricsPlugin"; }
        }

        public void OnCurrentSongChanged(object sender, CurrentSongEventArgs args)
        {
#warning To Do: load the lyrics and display them
        }

        public void OnStreamUpdate(object sender, StreamUpdateEventArgs args)
        {
        }

        public LyricsPlugin()
        {
            InitializeComponent();
        }
    }
}
