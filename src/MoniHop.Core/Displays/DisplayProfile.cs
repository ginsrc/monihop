namespace MoniHop.Core.Displays;

public sealed record DisplayProfile(
    string StableId,
    string? CustomName,
    string LastSystemName,
    string LastResolution,
    bool IsBuiltIn,
    DateTimeOffset LastSeenUtc,
    int? LastRefreshRateHz = null,
    int? LastScalePercent = null,
    DisplayOrientation LastOrientation = DisplayOrientation.Unknown,
    int? LastPhysicalWidthMillimeters = null,
    int? LastPhysicalHeightMillimeters = null)
{
    public DisplayProfile Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(StableId);
        ArgumentException.ThrowIfNullOrWhiteSpace(LastSystemName);
        ArgumentException.ThrowIfNullOrWhiteSpace(LastResolution);
        return this;
    }
}

public sealed record DisplayProfileState(
    DisplayProfile Profile,
    DisplaySnapshot? CurrentDisplay)
{
    public bool IsConnected => CurrentDisplay is not null;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Profile.CustomName)
            ? Profile.LastSystemName
            : Profile.CustomName;

    public string ConnectionStatus => IsConnected ? "已连接" : "未连接";
}
