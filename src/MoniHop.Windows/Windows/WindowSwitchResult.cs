using MoniHop.Core.Displays;

namespace MoniHop.Windows.Windows;

public enum WindowSwitchResult
{
    Moved,
    NoTarget,
    NoWindow,
}

public readonly record struct WindowSwitchOutcome(
    WindowSwitchResult Result,
    PixelRect? TargetWindowRect = null);
