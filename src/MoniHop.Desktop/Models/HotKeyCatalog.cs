using MoniHop.Windows.HotKeys;

namespace MoniHop.Desktop.Models;

public enum HotKeyCommand
{
    CursorNext,
    CursorPrevious,
    CursorCenterActiveDisplay,
    WindowPrevious,
    WindowNext,
    WindowAndCursorPrevious,
    WindowAndCursorNext,
    OpenProjectionPanel,
    ProjectWindowToDefaultDisplay,
    ProjectWindowToSpecificDisplay,
    ProjectWindowKeepSize,
    ProjectWindowMaximized,
    ProjectWindowLeftHalf,
    ProjectWindowRightHalf,
    RecallOffscreenWindows,
    ToggleApplicationProjection,
    OpenSettings,
}

public sealed record HotKeyActionDefinition(
    string Id,
    HotKeyCommand Command,
    string Category,
    string ActionName,
    string Description,
    bool IsImplemented = true,
    HotKeyGesture? DefaultGesture = null,
    string? TargetDisplayId = null,
    bool IsTargetAvailable = true);

public sealed record HotKeyDisplayTarget(string StableId, string Name, bool IsAvailable);

public static class HotKeyCatalog
{
    public static IReadOnlyList<HotKeyActionDefinition> All { get; } =
    [
        Definition(
            "cursor.next", HotKeyCommand.CursorNext, "鼠标", "鼠标切到下一屏",
            "保持当前相对位置并循环切换显示器。",
            new HotKeyGesture(HotKeyModifiers.Control | HotKeyModifiers.Alt, 0x4D)),
        Definition(
            "cursor.previous", HotKeyCommand.CursorPrevious, "鼠标", "鼠标切到上一屏",
            "反向循环切换鼠标所在显示器。"),
        Definition(
            "cursor.center", HotKeyCommand.CursorCenterActiveDisplay, "鼠标", "鼠标移到当前屏幕中央",
            "在鼠标位置不易发现时快速定位。"),
        Definition(
            "window.previous", HotKeyCommand.WindowPrevious, "窗口", "当前窗口移到上一屏",
            "保留窗口尺寸、位置比例和最大化状态。",
            new HotKeyGesture(HotKeyModifiers.Control | HotKeyModifiers.Alt | HotKeyModifiers.Shift, 0x25)),
        Definition(
            "window.next", HotKeyCommand.WindowNext, "窗口", "当前窗口移到下一屏",
            "保留窗口尺寸、位置比例和最大化状态。",
            new HotKeyGesture(HotKeyModifiers.Control | HotKeyModifiers.Alt | HotKeyModifiers.Shift, 0x27)),
        Definition(
            "window-cursor.previous", HotKeyCommand.WindowAndCursorPrevious, "窗口", "当前窗口和鼠标移到上一屏",
            "窗口移动成功后，将鼠标落在窗口可见区域。"),
        Definition(
            "window-cursor.next", HotKeyCommand.WindowAndCursorNext, "窗口", "当前窗口和鼠标移到下一屏",
            "窗口移动成功后，将鼠标落在窗口可见区域。"),
        Definition(
            "projection.panel", HotKeyCommand.OpenProjectionPanel, "快捷投放", "打开快捷投放面板",
            "选择目标显示器以及保持尺寸、最大化或半屏布局。"),
        Definition(
            "projection.default", HotKeyCommand.ProjectWindowToDefaultDisplay, "快捷投放", "当前窗口投放到默认目标",
            "使用窗口投放页配置的默认显示器和默认布局。"),
        Definition(
            "projection.keep-size", HotKeyCommand.ProjectWindowKeepSize, "快捷投放", "当前窗口保持尺寸投放",
            "投放到默认目标并保留窗口尺寸和相对位置。"),
        Definition(
            "projection.maximized", HotKeyCommand.ProjectWindowMaximized, "快捷投放", "当前窗口最大化投放",
            "投放到默认目标并最大化；固定尺寸窗口不会被强制放大。"),
        Definition(
            "projection.left-half", HotKeyCommand.ProjectWindowLeftHalf, "快捷投放", "当前窗口左半屏投放",
            "投放到默认目标的左半区域。"),
        Definition(
            "projection.right-half", HotKeyCommand.ProjectWindowRightHalf, "快捷投放", "当前窗口右半屏投放",
            "投放到默认目标的右半区域。"),
        Definition(
            "recovery.recall", HotKeyCommand.RecallOffscreenWindows, "恢复与程序", "召回屏幕外窗口",
            "只召回与当前所有显示器工作区完全无交集的普通窗口。"),
        Definition(
            "application-projection.toggle", HotKeyCommand.ToggleApplicationProjection, "恢复与程序", "暂停或恢复新窗口自动投放",
            "只切换新窗口自动投放，不影响主动快捷键和拖拽投放。"),
        Definition(
            "settings.open", HotKeyCommand.OpenSettings, "恢复与程序", "打开 MoniHop 设置",
            "显示或激活当前设置窗口。"),
    ];

    public static HotKeyActionDefinition Get(HotKeyCommand command) =>
        All.Single(item => item.Command == command);

    public static IReadOnlyList<HotKeyActionDefinition> CreateForDisplays(
        IEnumerable<HotKeyDisplayTarget> displays)
    {
        ArgumentNullException.ThrowIfNull(displays);
        var targets = displays
            .Where(item => !string.IsNullOrWhiteSpace(item.StableId))
            .GroupBy(item => item.StableId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Select(item => new HotKeyActionDefinition(
                $"projection.display:{item.StableId}",
                HotKeyCommand.ProjectWindowToSpecificDisplay,
                "快捷投放",
                $"投放到{item.Name}",
                "将当前窗口投放到这块显示器，并使用默认布局。",
                TargetDisplayId: item.StableId,
                IsTargetAvailable: item.IsAvailable));
        return All.Concat(targets).ToArray();
    }

    private static HotKeyActionDefinition Definition(
        string id,
        HotKeyCommand command,
        string category,
        string actionName,
        string description,
        HotKeyGesture? defaultGesture = null) =>
        new(id, command, category, actionName, description, DefaultGesture: defaultGesture);
}
