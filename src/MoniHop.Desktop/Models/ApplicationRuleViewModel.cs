using MoniHop.Core.ApplicationProjection;

namespace MoniHop.Desktop.Models;

public sealed record ApplicationRuleViewModel(
    ApplicationProjectionRule Rule,
    string ApplicationName,
    string TargetDisplayName,
    string LayoutName,
    string State,
    bool IsTargetAvailable)
{
    public static ApplicationRuleViewModel Create(
        ApplicationProjectionRule rule,
        string? targetDisplayName,
        bool isTargetAvailable)
    {
        ArgumentNullException.ThrowIfNull(rule);
        var state = rule.IsEnabled ? "已启用" : "已暂停";
        if (!isTargetAvailable)
        {
            state += " · 目标不可用";
        }

        return new ApplicationRuleViewModel(
            rule,
            rule.DisplayName,
            isTargetAvailable ? targetDisplayName ?? rule.TargetDisplayId : "目标不可用",
            FormatLayout(rule.Layout),
            state,
            isTargetAvailable);
    }

    public static string FormatLayout(ProjectionLayout layout) => layout switch
    {
        ProjectionLayout.Maximized => "最大化",
        ProjectionLayout.LeftHalf => "左半屏",
        ProjectionLayout.RightHalf => "右半屏",
        _ => "保持尺寸",
    };
}
