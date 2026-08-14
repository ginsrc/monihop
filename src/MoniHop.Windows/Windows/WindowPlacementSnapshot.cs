using MoniHop.Core.Displays;

namespace MoniHop.Windows.Windows;

public readonly record struct WindowPlacementSnapshot(
    uint ShowCommand,
    PixelRect WindowRect,
    PixelRect NormalRect)
{
    public bool IsMaximized => ShowCommand == 3;
}

public readonly record struct WindowCapabilities(bool CanResize, bool CanMaximize)
{
    public static WindowCapabilities Standard { get; } = new(true, true);
}
