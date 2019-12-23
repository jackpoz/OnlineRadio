using System.Windows.Controls;

namespace OnlineRadio.Core
{
    public interface IVisualPlugin : IPlugin
    {
        UserControl Control { get; }

        int ColumnSpan { get; }
    }
}
