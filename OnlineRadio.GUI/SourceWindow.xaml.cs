using System.Windows;

namespace OnlineRadio.GUI
{
    /// <summary>
    /// Interaction logic for SourceWindow.xaml
    /// </summary>
    public partial class SourceWindow : Window
    {
        public Source SourceResult
        {
            get;
            set;
        }

        public SourceWindow(Source SourceInput) : this()
        {
            NameBox.Text = SourceInput.Name;
            UrlBox.Text = SourceInput.Url;
            ArtistTitleOrderInvertedBox.IsChecked = SourceInput.ArtistTitleOrderInverted;
        }

        public SourceWindow()
        {
            InitializeComponent();
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            SourceResult = new Source(NameBox.Text, UrlBox.Text, ArtistTitleOrderInvertedBox.IsChecked ?? false);
            DialogResult = true;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
