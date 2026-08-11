using MoniHop.Core.Displays;

namespace MoniHop.Windows.Windows;

public readonly record struct WindowPlacementSnapshot(
    uint ShowCommand,
    PixelRect WindowRect,
    PixelRect NormalRect)
{
    public bool IsMaximized => ShowCommand == 3;
}
