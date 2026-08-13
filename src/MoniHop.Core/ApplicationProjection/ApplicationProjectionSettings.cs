namespace MoniHop.Core.ApplicationProjection;

public sealed record ApplicationProjectionSettings
{
    public static ApplicationProjectionSettings Default { get; } = new(false, null, []);

    public ApplicationProjectionSettings(
        bool isEnabled,
        string? defaultTargetDisplayId,
        IReadOnlyList<ApplicationProjectionRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        IsEnabled = isEnabled;
        DefaultTargetDisplayId = string.IsNullOrWhiteSpace(defaultTargetDisplayId)
            ? null
            : defaultTargetDisplayId.Trim();
        Rules = rules.ToArray();
    }

    public bool IsEnabled { get; }

    public string? DefaultTargetDisplayId { get; }

    public IReadOnlyList<ApplicationProjectionRule> Rules { get; }
}

public sealed record ApplicationProjectionRule
{
    public ApplicationProjectionRule(
        ApplicationIdentity application,
        string displayName,
        string targetDisplayId,
        ProjectionLayout layout,
        bool isEnabled)
    {
        Application = application ?? throw new ArgumentNullException(nameof(application));
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDisplayId);
        DisplayName = displayName.Trim();
        TargetDisplayId = targetDisplayId.Trim();
        Layout = layout;
        IsEnabled = isEnabled;
    }

    public ApplicationIdentity Application { get; }

    public string DisplayName { get; }

    public string TargetDisplayId { get; }

    public ProjectionLayout Layout { get; }

    public bool IsEnabled { get; }
}

public enum ProjectionLayout
{
    KeepSize,
    Maximized,
    LeftHalf,
    RightHalf,
}
