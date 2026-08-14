using MoniHop.Core.ApplicationProjection;

namespace MoniHop.Core.WindowProjection;

public sealed record WindowProjectionSettings
{
    public static WindowProjectionSettings Default { get; } = new(
        false,
        WindowProjectionTriggerMode.EdgeDwell,
        true,
        new RelativePosition(0.5, 0),
        null,
        ProjectionLayout.Maximized);

    public WindowProjectionSettings(
        bool isEnabled,
        WindowProjectionTriggerMode triggerMode,
        bool isPositionLocked,
        RelativePosition relativePosition,
        string? defaultTargetDisplayId = null,
        ProjectionLayout defaultLayout = ProjectionLayout.Maximized)
    {
        if (!Enum.IsDefined(triggerMode))
        {
            throw new ArgumentOutOfRangeException(nameof(triggerMode));
        }

        if (defaultLayout is not (ProjectionLayout.KeepSize or ProjectionLayout.Maximized or ProjectionLayout.LeftHalf or ProjectionLayout.RightHalf))
        {
            throw new ArgumentOutOfRangeException(nameof(defaultLayout));
        }

        IsEnabled = isEnabled;
        TriggerMode = triggerMode;
        IsPositionLocked = isPositionLocked;
        RelativePosition = relativePosition.Clamp();
        DefaultTargetDisplayId = string.IsNullOrWhiteSpace(defaultTargetDisplayId)
            ? null
            : defaultTargetDisplayId.Trim();
        DefaultLayout = defaultLayout;
    }

    public bool IsEnabled { get; }

    public WindowProjectionTriggerMode TriggerMode { get; }

    public bool IsPositionLocked { get; }

    public RelativePosition RelativePosition { get; }

    public string? DefaultTargetDisplayId { get; }

    public ProjectionLayout DefaultLayout { get; }
}

public enum WindowProjectionTriggerMode
{
    EdgeDwell,
    Immediate,
}

public readonly record struct RelativePosition(double X, double Y)
{
    public RelativePosition Clamp() => new(
        double.IsFinite(X) ? Math.Clamp(X, 0d, 1d) : 0.5d,
        double.IsFinite(Y) ? Math.Clamp(Y, 0d, 1d) : 0.5d);
}

public readonly record struct PixelSize
{
    public PixelSize(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        Width = width;
        Height = height;
    }

    public int Width { get; }

    public int Height { get; }
}
