using MoniHop.Core.Displays;

namespace MoniHop.Core.Windows;

public readonly record struct WindowSwitchPlan(
    DisplaySnapshot TargetDisplay,
    PixelRect TargetNormalRect);
