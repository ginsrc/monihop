using MoniHop.Core.ApplicationProjection;
using MoniHop.Core.Displays;

namespace MoniHop.Core.WindowProjection;

public sealed class WindowProjectionPlanner
{
    private const int DisplayAreaWidth = 120;
    private const int LayoutAreaHeight = 44;

    public PixelRect CalculateOverlayBounds(
        PixelRect workingArea,
        PixelSize overlaySize,
        RelativePosition relativePosition)
    {
        ValidateWorkingArea(workingArea);
        var position = relativePosition.Clamp();
        var width = Math.Min(overlaySize.Width, workingArea.Width);
        var height = Math.Min(overlaySize.Height, workingArea.Height);
        var centerX = workingArea.Left + (int)Math.Round(position.X * workingArea.Width);
        var centerY = workingArea.Top + (int)Math.Round(position.Y * workingArea.Height);
        var left = Math.Clamp(
            centerX - (width / 2),
            workingArea.Left,
            workingArea.Right - width);
        var top = Math.Clamp(
            centerY - (height / 2),
            workingArea.Top,
            workingArea.Bottom - height);
        return new PixelRect(left, top, left + width, top + height);
    }

    public PixelRect CalculatePortalBounds(
        PixelRect displayBounds,
        PixelSize portalSize,
        RelativePosition relativePosition)
    {
        ValidateWorkingArea(displayBounds);
        var position = relativePosition.Clamp();
        var width = Math.Min(portalSize.Width, displayBounds.Width);
        var height = Math.Min(portalSize.Height, displayBounds.Height);
        var centerX = displayBounds.Left + (int)Math.Round(position.X * displayBounds.Width);
        var left = Math.Clamp(
            centerX - (width / 2),
            displayBounds.Left,
            displayBounds.Right - width);
        return new PixelRect(left, displayBounds.Top, left + width, displayBounds.Top + height);
    }

    public RelativePosition ToRelativePosition(PixelRect workingArea, PixelRect overlayBounds)
    {
        ValidateWorkingArea(workingArea);
        var centerX = overlayBounds.Left + (overlayBounds.Width / 2d);
        var centerY = overlayBounds.Top + (overlayBounds.Height / 2d);
        return new RelativePosition(
            (centerX - workingArea.Left) / workingArea.Width,
            (centerY - workingArea.Top) / workingArea.Height).Clamp();
    }

    public PixelRect CalculateExpandedBounds(
        PixelRect workingArea,
        PixelSize panelSize,
        PixelRect portalBounds)
    {
        ValidateWorkingArea(workingArea);
        var width = Math.Min(panelSize.Width, workingArea.Width);
        var height = Math.Min(panelSize.Height, workingArea.Height);
        var portalCenterX = portalBounds.Left + (portalBounds.Width / 2);
        var left = Math.Clamp(portalCenterX - (width / 2), workingArea.Left, workingArea.Right - width);
        var top = Math.Clamp(portalBounds.Bottom, workingArea.Top, workingArea.Bottom - height);
        return new PixelRect(left, top, left + width, top + height);
    }

    public PixelRect CalculateExpandedOverlayBounds(PixelRect portalBounds, PixelRect panelBounds) =>
        new(
            Math.Min(portalBounds.Left, panelBounds.Left),
            Math.Min(portalBounds.Top, panelBounds.Top),
            Math.Max(portalBounds.Right, panelBounds.Right),
            Math.Max(portalBounds.Bottom, panelBounds.Bottom));

    public WindowProjectionCommandLayout CreateCommandLayout(
        PixelRect portalBounds,
        PixelRect panelBounds,
        IReadOnlyList<DisplaySnapshot> displays,
        DisplaySnapshot selectedDisplay)
    {
        ArgumentNullException.ThrowIfNull(displays);
        ArgumentNullException.ThrowIfNull(selectedDisplay);
        if (panelBounds.Width <= DisplayAreaWidth || panelBounds.Height <= LayoutAreaHeight)
        {
            throw new ArgumentOutOfRangeException(nameof(panelBounds));
        }
        if (displays.Count == 0)
        {
            throw new ArgumentException("At least one display is required.", nameof(displays));
        }

        var contentRight = panelBounds.Right - DisplayAreaWidth;
        var layoutTop = panelBounds.Bottom - LayoutAreaHeight;
        var defaultDropBounds = new PixelRect(panelBounds.Left, panelBounds.Top, contentRight, layoutTop);
        var layouts = new[]
        {
            ProjectionLayout.KeepSize,
            ProjectionLayout.Maximized,
            ProjectionLayout.LeftHalf,
            ProjectionLayout.RightHalf,
        };
        var layoutZones = new List<WindowProjectionLayoutZone>(layouts.Length);
        for (var index = 0; index < layouts.Length; index++)
        {
            var left = panelBounds.Left + ((contentRight - panelBounds.Left) * index / layouts.Length);
            var right = panelBounds.Left + ((contentRight - panelBounds.Left) * (index + 1) / layouts.Length);
            layoutZones.Add(new WindowProjectionLayoutZone(
                layouts[index],
                new PixelRect(left, layoutTop, right, panelBounds.Bottom)));
        }

        var displayZones = new List<WindowProjectionDisplayZone>(displays.Count);
        for (var index = 0; index < displays.Count; index++)
        {
            var top = panelBounds.Top + (panelBounds.Height * index / displays.Count);
            var bottom = panelBounds.Top + (panelBounds.Height * (index + 1) / displays.Count);
            var display = displays[index];
            displayZones.Add(new WindowProjectionDisplayZone(
                display,
                new PixelRect(contentRight, top, panelBounds.Right, bottom),
                StringComparer.OrdinalIgnoreCase.Equals(display.StableId, selectedDisplay.StableId)));
        }

        return new WindowProjectionCommandLayout(
            portalBounds,
            panelBounds,
            defaultDropBounds,
            layoutZones,
            displayZones);
    }

    public WindowProjectionHit HitTest(WindowProjectionCommandLayout command, PixelPoint point)
    {
        ArgumentNullException.ThrowIfNull(command);
        foreach (var zone in command.LayoutZones)
        {
            if (zone.Bounds.Contains(point))
            {
                return new WindowProjectionHit(WindowProjectionHitKind.Layout, zone.Layout);
            }
        }
        foreach (var zone in command.DisplayZones)
        {
            if (zone.Bounds.Contains(point))
            {
                return new WindowProjectionHit(WindowProjectionHitKind.Display, TargetDisplay: zone.TargetDisplay);
            }
        }
        return command.DefaultDropBounds.Contains(point)
            ? WindowProjectionHit.DefaultDrop
            : WindowProjectionHit.None;
    }

    public WindowProjectionPlan Plan(
        IReadOnlyList<DisplaySnapshot> displays,
        DisplaySnapshot targetDisplay,
        PixelRect sourceRect,
        ProjectionLayout layout)
    {
        ArgumentNullException.ThrowIfNull(displays);
        ArgumentNullException.ThrowIfNull(targetDisplay);
        var targetRect = ProjectionGeometry.CalculateTargetRect(displays, targetDisplay, sourceRect, layout);
        return new WindowProjectionPlan(
            targetDisplay,
            targetRect,
            layout,
            layout == ProjectionLayout.Maximized);
    }

    private static void ValidateWorkingArea(PixelRect workingArea)
    {
        if (workingArea.Width <= 0 || workingArea.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workingArea));
        }
    }
}

public sealed record WindowProjectionPlan(
    DisplaySnapshot TargetDisplay,
    PixelRect TargetRect,
    ProjectionLayout Layout,
    bool ShouldMaximize);
