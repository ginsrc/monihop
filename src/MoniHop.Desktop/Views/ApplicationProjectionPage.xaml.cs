using System.Collections.ObjectModel;
using System.Windows.Controls;
using MoniHop.Core.Displays;

namespace MoniHop.Desktop.Views;

public partial class ApplicationProjectionPage : UserControl
{
    public ApplicationProjectionPage(IReadOnlyList<DisplaySnapshot> displays)
    {
        InitializeComponent();
        DisplayTargets = new ObservableCollection<string>(displays.Select((_, index) => $"显示器 {index + 1}"));
        DisplayTargets.Insert(0, "跟随 Windows 主显示器（默认）");
        DataContext = this;
    }

    public ObservableCollection<string> DisplayTargets { get; }
}
