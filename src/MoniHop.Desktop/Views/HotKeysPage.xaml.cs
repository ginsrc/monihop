using System.Collections.ObjectModel;
using System.Windows.Controls;
using MoniHop.Desktop.Models;

namespace MoniHop.Desktop.Views;

public partial class HotKeysPage : UserControl
{
    public HotKeysPage(ObservableCollection<HotKeyStatusViewModel> hotKeys)
    {
        InitializeComponent();
        HotKeys = hotKeys ?? throw new ArgumentNullException(nameof(hotKeys));
        DataContext = this;
    }

    public ObservableCollection<HotKeyStatusViewModel> HotKeys { get; }
}
