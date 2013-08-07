using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
using System.Xml.Serialization;
using OnlineRadio.Core;

namespace OnlineRadio.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<Source> sources;
        const string sourcesPath = "sources.xml";
        Radio radio;
        const string windowTitle = "OnlineRadio";

        public MainWindow()
        {
            InitializeComponent();

            Title = windowTitle;

            if (!File.Exists(sourcesPath))
                sources = new ObservableCollection<Source>();
            else using (StreamReader sr = new StreamReader(sourcesPath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<Source>));
                sources = new ObservableCollection<Source>((List<Source>)serializer.Deserialize(sr));
            }

            SelectSourceCombo.ItemsSource = sources;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            using (StreamWriter sw = new StreamWriter(sourcesPath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<Source>));
                serializer.Serialize(sw, sources.ToList());
            }

            if (radio != null)
                radio.Dispose();
        }

        private void PlaySourceBtn_Click(object sender, RoutedEventArgs e)
        {
            //stop playing any current radio
            StopPlaying();

            if (SelectSourceCombo.SelectedValue == null)
            {
                LogMessage("Please select a radio station to play");
                return;
            }

            Source source = (Source)SelectSourceCombo.SelectedValue;
            radio = new Radio(source.Url);
            radio.OnCurrentSongChanged += (s, eventArgs) =>
            {
                Dispatcher.Invoke(() =>
                {
                    string message = eventArgs.NewSong.Artist + " - " + eventArgs.NewSong.Title;
                    LogMessage(message);
                    Title = windowTitle + " - " + message;
                    InfoArtistLbl.Content = eventArgs.NewSong.Artist;
                    InfoTitleLbl.Content = eventArgs.NewSong.Title;
                });
            };

            Radio.OnMessageLogged += (s, eventArgs) =>
            {
                Dispatcher.Invoke(() =>
                {
                    LogMessage(eventArgs.Message);
                });
            };
            radio.Start();
        }

        private void StopSourceBtn_Click(object sender, RoutedEventArgs e)
        {
            StopPlaying();
        }

        private void StopPlaying()
        {
            if (radio == null)
                return;

            Title = windowTitle;
            InfoArtistLbl.Content = String.Empty;
            InfoTitleLbl.Content = String.Empty;

            radio.Stop();
            radio = null;
        }

        private void AddSourceBtn_Click(object sender, RoutedEventArgs e)
        {
            SourceWindow window = new SourceWindow();
            window.Owner = this;
            window.Title = "Add new source";
            if (window.ShowDialog() == true)
            {
                sources.Add(window.SourceResult);
            }
        }

        private void EditSourceBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RemoveSourceBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void LogMessage(string message)
        {
        }
    }
}
