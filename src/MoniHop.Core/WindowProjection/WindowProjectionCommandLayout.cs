using MoniHop.Core.ApplicationProjection;
using MoniHop.Core.Displays;

namespace MoniHop.Core.WindowProjection;

public sealed record WindowProjectionCommandLayout(
    PixelRect PortalBounds,
    PixelRect PanelBounds,
    PixelRect DefaultDropBounds,
    IReadOnlyList<WindowProjectionLayoutZone> LayoutZones,
    IReadOnlyList<WindowProjectionDisplayZone> DisplayZones);

public readonly record struct WindowProjectionLayoutZone(
    ProjectionLayout Layout,
    PixelRect Bounds);

public readonly record struct WindowProjectionDisplayZone(
    DisplaySnapshot TargetDisplay,
    PixelRect Bounds,
    bool IsSelected);

public sealed record WindowProjectionHit(
    WindowProjectionHitKind Kind,
    ProjectionLayout? Layout = null,
    DisplaySnapshot? TargetDisplay = null)
{
    public static WindowProjectionHit None { get; } = new(WindowProjectionHitKind.None);

    public static WindowProjectionHit DefaultDrop { get; } = new(WindowProjectionHitKind.DefaultDrop);
}

public enum WindowProjectionHitKind
{
    None,
    DefaultDrop,
    Layout,
    Display,
}
