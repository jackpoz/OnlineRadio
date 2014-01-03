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
using System.Drawing;
using Point = System.Drawing.Point;

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
            var selectedSource = (from source in sources
                                  where source.Selected
                                  select source).FirstOrDefault();
            if (selectedSource != null)
                SelectSourceCombo.SelectedValue = selectedSource;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if(radio != null)
                radio.Dispose();

            using (StreamWriter sw = new StreamWriter(sourcesPath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<Source>));
                serializer.Serialize(sw, sources.ToList());
            }
        }

        private async void PlaySourceBtn_Click(object sender, RoutedEventArgs e)
        {
            //stop playing any current radio
            await StopPlaying();

            if (SelectSourceCombo.SelectedValue == null)
            {
                LogMessage("Please select a radio station to play");
                return;
            }

            LogMessage("Starting playing...");

            foreach (Source sourceChoice in SelectSourceCombo.Items)
                sourceChoice.Selected = false;

            Source source = (Source)SelectSourceCombo.SelectedValue;
            source.Selected = true;
            radio = new Radio(source.Url);
            radio.OnCurrentSongChanged += (s, eventArgs) =>
            {
                Dispatcher.InvokeAsync(() =>
                {
                    string message = eventArgs.NewSong.Artist + " - " + eventArgs.NewSong.Title;
                    LogMessage(message);
                    Title = windowTitle + " - " + message;
                    InfoArtistTxt.Text = eventArgs.NewSong.Artist;
                    InfoTitleTxt.Text = eventArgs.NewSong.Title;
                });
            };

            Radio.OnMessageLogged += (s, eventArgs) =>
            {
                Dispatcher.InvokeAsync(() =>
                {
                    LogMessage(eventArgs.Message);
                });
            };

            radio.OnPluginsLoaded += (s, eventArgs) =>
            {
                Dispatcher.InvokeAsync(() =>
                {
                    AddPluginControls(s, eventArgs);
                });
            };

            radio.Start();
        }

        private async void StopSourceBtn_Click(object sender, RoutedEventArgs e)
        {
            await StopPlaying();
        }

        private async Task StopPlaying()
        {
            if (radio == null)
                return;

            Title = windowTitle;
            InfoArtistTxt.Text = String.Empty;
            InfoTitleTxt.Text = String.Empty;
            PluginsGrid.Children.Clear();

            LogMessage("Stopping playing...");
            await Task.Run(() => radio.Stop());
            LogMessage("Playing stopped");
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
            if (SelectSourceCombo.SelectedValue == null)
                return;

            Source source = (Source)SelectSourceCombo.SelectedValue;
            SourceWindow window = new SourceWindow(source);
            window.Owner = this;
            window.Title = "Edit source";
            if (window.ShowDialog() == true)
            {
                //replace the Source with the result one from SourceWindow
                int index = sources.IndexOf(source);
                sources[index] = window.SourceResult;
                SelectSourceCombo.SelectedItem = window.SourceResult;
            }
        }

        private void RemoveSourceBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SelectSourceCombo.SelectedValue == null)
                return;

            sources.Remove((Source)SelectSourceCombo.SelectedValue);
        }

        private void LogMessage(string message)
        {
            statusBarTxt.Text = message;
        }

        private void AddPluginControls(object sender, PluginEventArgs e)
        {
            GridCells cells = new GridCells();

            foreach (IPlugin plugin in e.Plugins)
	        {
                var visualPlugin = plugin as IVisualPlugin;
                if (visualPlugin != null)
                {
                    Point freeCell = cells.GetFreeCell(visualPlugin.ColumnSpan);
                    if (freeCell.Y >= PluginsGrid.RowDefinitions.Count)
                        PluginsGrid.RowDefinitions.Add(new RowDefinition());
                    PluginsGrid.Children.Add(visualPlugin.Control);
                    Grid.SetColumnSpan(visualPlugin.Control, visualPlugin.ColumnSpan);
                    Grid.SetColumn(visualPlugin.Control, freeCell.X);
                    Grid.SetRow(visualPlugin.Control, freeCell.Y);
                }
	        }
        }

        class GridCells
        {
            Point lastSingleCell;
            Point lastDoubleCell;

            public GridCells()
            {
                lastSingleCell = new Point();
                lastDoubleCell = new Point();
            }

            public Point GetFreeCell(int columnSpan)
            {
                Point result;

                if (columnSpan == 1)
                {
                    result = lastSingleCell;
                    if (lastSingleCell.X == 0)
                    {
                        lastSingleCell.X = 1;
                        lastDoubleCell.Y += 1;
                    }
                    else
                    {
                        lastSingleCell.Y = lastDoubleCell.Y;
                        lastSingleCell.X = 0;
                    }
                }
                else
                {
                    result = lastDoubleCell;
                    lastDoubleCell.Y += 1;
                    if (lastSingleCell.X == 0)
                        lastSingleCell.Y += 1;
                }

                return result;
            }
        }
    }
}
