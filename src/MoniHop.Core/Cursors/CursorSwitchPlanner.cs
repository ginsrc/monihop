using MoniHop.Core.Displays;
using MoniHop.Core.Windows;

namespace MoniHop.Core.Cursors;

public sealed class CursorSwitchPlanner
{
    public PixelPoint? PlanNext(
        IReadOnlyList<DisplaySnapshot> displays,
        PixelPoint currentPosition) =>
        Plan(displays, currentPosition, DisplayDirection.Next);

    public PixelPoint? Plan(
        IReadOnlyList<DisplaySnapshot> displays,
        PixelPoint currentPosition,
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

        var sourceIndex = Array.FindIndex(
            orderedDisplays,
            display => display.Bounds.Contains(currentPosition));

        if (sourceIndex < 0)
        {
            return null;
        }

        var source = orderedDisplays[sourceIndex].Bounds;
        var targetIndex = (sourceIndex + (int)direction + orderedDisplays.Length) % orderedDisplays.Length;
        var target = orderedDisplays[targetIndex].Bounds;
        var relativeX = (double)(currentPosition.X - source.Left) / source.Width;
        var relativeY = (double)(currentPosition.Y - source.Top) / source.Height;
        var mappedX = target.Left + (int)Math.Floor(relativeX * target.Width);
        var mappedY = target.Top + (int)Math.Floor(relativeY * target.Height);

        return new PixelPoint(
            Math.Clamp(mappedX, target.Left, target.Right - 1),
            Math.Clamp(mappedY, target.Top, target.Bottom - 1));
    }

    public PixelPoint? PlanCenter(
        IReadOnlyList<DisplaySnapshot> displays,
        PixelPoint currentPosition)
    {
        ArgumentNullException.ThrowIfNull(displays);
        var display = displays.FirstOrDefault(item => item.Bounds.Contains(currentPosition));
        return display is null
            ? null
            : new PixelPoint(
                display.Bounds.Left + (display.Bounds.Width / 2),
                display.Bounds.Top + (display.Bounds.Height / 2));
    }
}
