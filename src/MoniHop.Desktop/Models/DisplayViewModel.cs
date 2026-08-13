using MoniHop.Core.Displays;

namespace MoniHop.Desktop.Models;

public sealed record DisplayViewModel(
    string ScreenLabel,
    string Name,
    string Resolution,
    string PrimaryStatus,
    string DeviceName)
{
    public static IReadOnlyList<DisplayViewModel> CreateAll(
        IReadOnlyList<DisplaySnapshot> displays)
    {
        ArgumentNullException.ThrowIfNull(displays);

        return displays
            .Select((display, index) => new DisplayViewModel(
                $"屏幕 {index + 1}",
                display.DisplayName,
                $"{display.Bounds.Width} × {display.Bounds.Height}",
                display.IsPrimary ? "Windows 主显示器" : "普通显示器",
                display.DeviceName))
            .ToArray();
    }
}
