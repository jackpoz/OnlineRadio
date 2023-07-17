using OnlineRadio.Core;
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
    /// Interaction logic for DecreaseVolumeButton.xaml
    /// </summary>
    public partial class DecreaseVolumeButton : UserControl
    {
        Radio _radio;

        public DecreaseVolumeButton(Radio radio)
        {
            _radio = radio;

            InitializeComponent();
        }

        private void ChangeVolumeBtn_Click(object sender, RoutedEventArgs e)
        {
            _radio.Volume -= 0.05f;
        }
    }
}
