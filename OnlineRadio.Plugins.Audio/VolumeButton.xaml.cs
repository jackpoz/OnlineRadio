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

namespace OnlineRadio.Plugins.Audio
{
    /// <summary>
    /// Interaction logic for VolumeButton.xaml
    /// </summary>
    public partial class VolumeButton : UserControl
    {
        public VolumeButton()
        {
            InitializeComponent();
        }

        private void ChangeVolumeBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Not implemented yet");
        }
    }
}
