namespace MoniHop.Core.Displays;

public sealed record DisplaySnapshot
{
    public DisplaySnapshot(
        string deviceName,
        string displayName,
        PixelRect bounds,
        PixelRect workingArea,
        bool isPrimary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bounds));
        }

        if (!bounds.Contains(workingArea))
        {
            throw new ArgumentOutOfRangeException(nameof(workingArea));
        }

        DeviceName = deviceName;
        DisplayName = displayName;
        Bounds = bounds;
        WorkingArea = workingArea;
        IsPrimary = isPrimary;
    }

    public string DeviceName { get; }

    public string DisplayName { get; }

    public PixelRect Bounds { get; }

    public PixelRect WorkingArea { get; }

    public bool IsPrimary { get; }
}
