using MoniHop.Core.Displays;

namespace MoniHop.Core.ApplicationProjection;

public sealed class ApplicationProjectionPlanner
{
    public ApplicationProjectionPlan? Plan(
        ApplicationProjectionSettings settings,
        ApplicationIdentity application,
        IReadOnlyList<DisplaySnapshot> displays,
        PixelRect currentRect)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(displays);

        if (!settings.IsEnabled || displays.Count < 2)
        {
            return null;
        }

        var rule = settings.Rules.FirstOrDefault(item =>
            item.IsEnabled && item.Application.Matches(application));
        var targetId = rule?.TargetDisplayId ?? settings.DefaultTargetDisplayId;
        var target = targetId is null
            ? displays.FirstOrDefault(display => display.IsPrimary)
            : displays.FirstOrDefault(display =>
                StringComparer.OrdinalIgnoreCase.Equals(display.StableId, targetId));
        var usedPrimaryFallback = target is null && targetId is not null;
        target ??= displays.FirstOrDefault(display => display.IsPrimary) ?? displays[0];

        var layout = rule?.Layout ?? ProjectionLayout.KeepSize;
        var targetRect = ProjectionGeometry.CalculateTargetRect(displays, target, currentRect, layout);
        return new ApplicationProjectionPlan(
            target,
            targetRect,
            layout,
            layout == ProjectionLayout.Maximized,
            rule is null ? ProjectionRuleSource.Global : ProjectionRuleSource.Application,
            usedPrimaryFallback);
    }

}

public sealed record ApplicationProjectionPlan(
    DisplaySnapshot TargetDisplay,
    PixelRect TargetRect,
    ProjectionLayout Layout,
    bool ShouldMaximize,
    ProjectionRuleSource RuleSource,
    bool UsedPrimaryFallback);

public enum ProjectionRuleSource
{
    Global,
    Application,
}
