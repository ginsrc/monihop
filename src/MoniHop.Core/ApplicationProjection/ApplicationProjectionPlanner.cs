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
        var targetRect = CalculateTargetRect(displays, target, currentRect, layout);
        return new ApplicationProjectionPlan(
            target,
            targetRect,
            layout,
            layout == ProjectionLayout.Maximized,
            rule is null ? ProjectionRuleSource.Global : ProjectionRuleSource.Application,
            usedPrimaryFallback);
    }

    private static PixelRect CalculateTargetRect(
        IReadOnlyList<DisplaySnapshot> displays,
        DisplaySnapshot target,
        PixelRect currentRect,
        ProjectionLayout layout)
    {
        var workArea = target.WorkingArea;
        return layout switch
        {
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
            _ => MapKeepSize(displays, target, currentRect),
        };
    }

    private static PixelRect MapKeepSize(
        IReadOnlyList<DisplaySnapshot> displays,
        DisplaySnapshot target,
        PixelRect currentRect)
    {
        var centerX = currentRect.Left + (currentRect.Width / 2);
        var centerY = currentRect.Top + (currentRect.Height / 2);
        var source = displays.FirstOrDefault(display =>
            display.Bounds.Contains(new PixelPoint(centerX, centerY)));
        var targetArea = target.WorkingArea;
        var left = targetArea.Left;
        var top = targetArea.Top;
        if (source is not null)
        {
            var sourceArea = source.WorkingArea;
            var relativeX = (double)(currentRect.Left - sourceArea.Left) / sourceArea.Width;
            var relativeY = (double)(currentRect.Top - sourceArea.Top) / sourceArea.Height;
            left += (int)Math.Floor(relativeX * targetArea.Width);
            top += (int)Math.Floor(relativeY * targetArea.Height);
        }

        var width = Math.Min(currentRect.Width, targetArea.Width);
        var height = Math.Min(currentRect.Height, targetArea.Height);
        left = Math.Clamp(left, targetArea.Left, targetArea.Right - width);
        top = Math.Clamp(top, targetArea.Top, targetArea.Bottom - height);
        return new PixelRect(left, top, left + width, top + height);
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
