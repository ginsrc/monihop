namespace MoniHop.Core.Displays;

public sealed record DisplaySnapshot
{
    public DisplaySnapshot(
        string deviceName,
        string displayName,
        PixelRect bounds,
        PixelRect workingArea,
        bool isPrimary,
        string? stableId = null,
        int? refreshRateHz = null,
        int? scalePercent = null,
        DisplayOrientation orientation = DisplayOrientation.Unknown,
        int? physicalWidthMillimeters = null,
        int? physicalHeightMillimeters = null,
        int? resolutionWidth = null,
        int? resolutionHeight = null)
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
        StableId = string.IsNullOrWhiteSpace(stableId) ? deviceName : stableId;
        Bounds = bounds;
        WorkingArea = workingArea;
        IsPrimary = isPrimary;
        RefreshRateHz = refreshRateHz;
        ScalePercent = scalePercent;
        Orientation = orientation;
        PhysicalWidthMillimeters = physicalWidthMillimeters;
        PhysicalHeightMillimeters = physicalHeightMillimeters;
        ResolutionWidth = resolutionWidth is > 0 ? resolutionWidth.Value : bounds.Width;
        ResolutionHeight = resolutionHeight is > 0 ? resolutionHeight.Value : bounds.Height;
    }

    public string DeviceName { get; }

    public string DisplayName { get; }

    public string StableId { get; }

    public PixelRect Bounds { get; }

    public PixelRect WorkingArea { get; }

    public bool IsPrimary { get; }

    public int? RefreshRateHz { get; }

    public int? ScalePercent { get; }

    public DisplayOrientation Orientation { get; }

    public int? PhysicalWidthMillimeters { get; }

    public int? PhysicalHeightMillimeters { get; }

    public int ResolutionWidth { get; }

    public int ResolutionHeight { get; }
}

public enum DisplayOrientation
{
    Unknown,
    Landscape,
    Portrait,
    LandscapeFlipped,
    PortraitFlipped,
}
