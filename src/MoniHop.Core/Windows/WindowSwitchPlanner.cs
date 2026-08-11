using MoniHop.Core.Displays;

namespace MoniHop.Core.Windows;

public sealed class WindowSwitchPlanner
{
    public WindowSwitchPlan? Plan(
        IReadOnlyList<DisplaySnapshot> displays,
        PixelRect normalRect,
        DisplayDirection direction)
    {
        ArgumentNullException.ThrowIfNull(displays);

        if (displays.Count < 2)
        {
            return null;
        }

        var orderedDisplays = displays
            .OrderBy(display => display.Bounds.Left)
            .ThenBy(display => display.Bounds.Top)
            .ThenBy(display => display.DeviceName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var center = new PixelPoint(
            normalRect.Left + (normalRect.Width / 2),
            normalRect.Top + (normalRect.Height / 2));
        var sourceIndex = Array.FindIndex(
            orderedDisplays,
            display => display.Bounds.Contains(center));

        if (sourceIndex < 0)
        {
            return null;
        }

        var targetIndex = (sourceIndex + (int)direction + orderedDisplays.Length) %
            orderedDisplays.Length;
        var source = orderedDisplays[sourceIndex].WorkingArea;
        var targetDisplay = orderedDisplays[targetIndex];
        var target = targetDisplay.WorkingArea;
        var relativeX = (double)(normalRect.Left - source.Left) / source.Width;
        var relativeY = (double)(normalRect.Top - source.Top) / source.Height;
        var mappedLeft = target.Left + (int)Math.Floor(relativeX * target.Width);
        var mappedTop = target.Top + (int)Math.Floor(relativeY * target.Height);
        var maxLeft = Math.Max(target.Left, target.Right - normalRect.Width);
        var maxTop = Math.Max(target.Top, target.Bottom - normalRect.Height);
        var clampedLeft = Math.Clamp(mappedLeft, target.Left, maxLeft);
        var clampedTop = Math.Clamp(mappedTop, target.Top, maxTop);

        return new WindowSwitchPlan(
            targetDisplay,
            new PixelRect(
                clampedLeft,
                clampedTop,
                clampedLeft + normalRect.Width,
                clampedTop + normalRect.Height));
    }
}
