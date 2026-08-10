using System.Collections.ObjectModel;
using System.Windows;
using MoniHop.Core.Displays;

namespace MoniHop.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(IReadOnlyList<DisplaySnapshot> displays)
    {
        ArgumentNullException.ThrowIfNull(displays);

        Displays = new ObservableCollection<DisplayItem>(
            displays.Select((display, index) => new DisplayItem(
                $"显示器 {index + 1}",
                display.DeviceName,
                $"{display.Bounds.Width} × {display.Bounds.Height}",
                display.IsPrimary ? Visibility.Visible : Visibility.Collapsed)));

        DataContext = this;
        InitializeComponent();
    }

    public ObservableCollection<DisplayItem> Displays { get; }

    public sealed record DisplayItem(
        string DisplayName,
        string DeviceName,
        string ResolutionText,
        Visibility PrimaryVisibility);
}
