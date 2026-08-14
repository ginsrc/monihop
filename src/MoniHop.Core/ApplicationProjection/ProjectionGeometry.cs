using MoniHop.Core.Displays;

namespace MoniHop.Core.ApplicationProjection;

public static class ProjectionGeometry
{
    public static PixelRect CalculateTargetRect(
        IReadOnlyList<DisplaySnapshot> displays,
        DisplaySnapshot target,
        PixelRect sourceRect,
        ProjectionLayout layout)
    {
        ArgumentNullException.ThrowIfNull(displays);
        ArgumentNullException.ThrowIfNull(target);
        if (sourceRect.Width <= 0 || sourceRect.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceRect));
        }

        var workArea = target.WorkingArea;
        return layout switch
        {
            ProjectionLayout.KeepSize => MapKeepSize(displays, target, sourceRect),
            ProjectionLayout.Maximized => workArea,
            ProjectionLayout.LeftHalf => new PixelRect(
                workArea.Left,
                workArea.Top,
                workArea.Left + (workArea.Width / 2),
                workArea.Bottom),
            ProjectionLayout.RightHalf => new PixelRect(
                workArea.Left + (workArea.Width / 2),
                workArea.Top,
                workArea.Right,
                workArea.Bottom),
            _ => throw new ArgumentOutOfRangeException(nameof(layout)),
        };
    }

    private static PixelRect MapKeepSize(
        IReadOnlyList<DisplaySnapshot> displays,
        DisplaySnapshot target,
        PixelRect sourceRect)
    {
        var centerX = sourceRect.Left + (sourceRect.Width / 2);
        var centerY = sourceRect.Top + (sourceRect.Height / 2);
        var source = displays.FirstOrDefault(display =>
            display.Bounds.Contains(new PixelPoint(centerX, centerY)));
        var targetArea = target.WorkingArea;
        var scale = Math.Min(
            1d,
            Math.Min(
                (double)targetArea.Width / sourceRect.Width,
                (double)targetArea.Height / sourceRect.Height));
        var width = Math.Max(1, (int)Math.Floor(sourceRect.Width * scale));
        var height = Math.Max(1, (int)Math.Floor(sourceRect.Height * scale));
        var left = targetArea.Left;
        var top = targetArea.Top;
        if (source is not null)
        {
            var sourceArea = source.WorkingArea;
            var relativeX = (double)(sourceRect.Left - sourceArea.Left) / sourceArea.Width;
            var relativeY = (double)(sourceRect.Top - sourceArea.Top) / sourceArea.Height;
            left += (int)Math.Floor(relativeX * targetArea.Width);
            top += (int)Math.Floor(relativeY * targetArea.Height);
        }

        left = Math.Clamp(left, targetArea.Left, targetArea.Right - width);
        top = Math.Clamp(top, targetArea.Top, targetArea.Bottom - height);
        return new PixelRect(left, top, left + width, top + height);
    }
}
