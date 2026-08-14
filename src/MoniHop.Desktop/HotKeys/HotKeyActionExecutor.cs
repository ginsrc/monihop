using MoniHop.Core.ApplicationProjection;
using MoniHop.Core.Windows;
using MoniHop.Desktop.Models;
using MoniHop.Desktop.Settings;
using MoniHop.Desktop.WindowProjection;
using MoniHop.Windows.Cursors;
using MoniHop.Windows.Windows;

namespace MoniHop.Desktop.HotKeys;

public sealed class HotKeyActionExecutor
{
    private readonly CursorSwitchService _cursorSwitchService;
    private readonly WindowSwitchService _windowSwitchService;
    private readonly WindowProjectionShortcutService _projectionService;
    private readonly OffscreenWindowRecallService _recallService;
    private readonly ApplicationProjectionSettingsService _applicationSettings;

    public HotKeyActionExecutor(
        CursorSwitchService cursorSwitchService,
        WindowSwitchService windowSwitchService,
        WindowProjectionShortcutService projectionService,
        OffscreenWindowRecallService recallService,
        ApplicationProjectionSettingsService applicationSettings)
    {
        _cursorSwitchService = cursorSwitchService ?? throw new ArgumentNullException(nameof(cursorSwitchService));
        _windowSwitchService = windowSwitchService ?? throw new ArgumentNullException(nameof(windowSwitchService));
        _projectionService = projectionService ?? throw new ArgumentNullException(nameof(projectionService));
        _recallService = recallService ?? throw new ArgumentNullException(nameof(recallService));
        _applicationSettings = applicationSettings ?? throw new ArgumentNullException(nameof(applicationSettings));
    }

    public event EventHandler<WindowProjectionCandidate>? ProjectionPanelRequested;

    public event EventHandler? SettingsRequested;

    public HotKeyExecutionResult Execute(
        HotKeyActionDefinition definition,
        nint excludedWindowHandle)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return definition.Command switch
        {
            HotKeyCommand.CursorNext => Cursor(_cursorSwitchService.Switch(DisplayDirection.Next)),
            HotKeyCommand.CursorPrevious => Cursor(_cursorSwitchService.Switch(DisplayDirection.Previous)),
            HotKeyCommand.CursorCenterActiveDisplay => Cursor(_cursorSwitchService.CenterCurrentDisplay()),
            HotKeyCommand.WindowPrevious => Window(_windowSwitchService.Switch(DisplayDirection.Previous, excludedWindowHandle)),
            HotKeyCommand.WindowNext => Window(_windowSwitchService.Switch(DisplayDirection.Next, excludedWindowHandle)),
            HotKeyCommand.WindowAndCursorPrevious => WindowAndCursor(DisplayDirection.Previous, excludedWindowHandle),
            HotKeyCommand.WindowAndCursorNext => WindowAndCursor(DisplayDirection.Next, excludedWindowHandle),
            HotKeyCommand.OpenProjectionPanel => OpenProjectionPanel(excludedWindowHandle),
            HotKeyCommand.ProjectWindowToDefaultDisplay => Projection(
                _projectionService.ProjectCurrent(excludedWindowHandle)),
            HotKeyCommand.ProjectWindowToSpecificDisplay => Projection(
                _projectionService.ProjectCurrent(
                    excludedWindowHandle,
                    targetDisplayId: definition.TargetDisplayId)),
            HotKeyCommand.ProjectWindowKeepSize => Projection(
                _projectionService.ProjectCurrent(excludedWindowHandle, ProjectionLayout.KeepSize)),
            HotKeyCommand.ProjectWindowMaximized => Projection(
                _projectionService.ProjectCurrent(excludedWindowHandle, ProjectionLayout.Maximized)),
            HotKeyCommand.ProjectWindowLeftHalf => Projection(
                _projectionService.ProjectCurrent(excludedWindowHandle, ProjectionLayout.LeftHalf)),
            HotKeyCommand.ProjectWindowRightHalf => Projection(
                _projectionService.ProjectCurrent(excludedWindowHandle, ProjectionLayout.RightHalf)),
            HotKeyCommand.RecallOffscreenWindows => Recall(),
            HotKeyCommand.ToggleApplicationProjection => ToggleApplicationProjection(),
            HotKeyCommand.OpenSettings => OpenSettings(),
            _ => new HotKeyExecutionResult(false, "动作不可用"),
        };
    }

    public WindowProjectionShortcutResult Project(
        WindowProjectionCandidate candidate,
        ProjectionLayout layout,
        string targetDisplayId) =>
        _projectionService.Project(candidate, layout, targetDisplayId);

    public IReadOnlyList<Core.Displays.DisplaySnapshot> ReadProjectionDisplays() =>
        _projectionService.ReadDisplays();

    public string? ResolveProjectionDefaultTargetId(WindowProjectionCandidate candidate) =>
        _projectionService.ResolveDefaultTargetId(candidate);

    private HotKeyExecutionResult WindowAndCursor(
        DisplayDirection direction,
        nint excludedWindowHandle)
    {
        var outcome = _windowSwitchService.SwitchDetailed(direction, excludedWindowHandle);
        if (outcome.Result == WindowSwitchResult.Moved && outcome.TargetWindowRect is { } targetRect)
        {
            _cursorSwitchService.MoveToCenter(targetRect);
        }

        return Window(outcome.Result);
    }

    private HotKeyExecutionResult OpenProjectionPanel(nint excludedWindowHandle)
    {
        var candidate = _projectionService.CaptureCurrent(excludedWindowHandle);
        if (candidate is null)
        {
            return new HotKeyExecutionResult(false, "当前窗口不可投放");
        }

        ProjectionPanelRequested?.Invoke(this, candidate);
        return new HotKeyExecutionResult(true, "请选择投放位置");
    }

    private HotKeyExecutionResult Recall()
    {
        var result = _recallService.Recall(0);
        return result.Status == OffscreenWindowRecallStatus.NoDisplay
            ? new HotKeyExecutionResult(false, "没有可用显示器")
            : new HotKeyExecutionResult(
                result.FailedCount == 0,
                result.FailedCount == 0
                    ? $"已召回 {result.MovedCount} 个窗口"
                    : $"已召回 {result.MovedCount} 个，{result.FailedCount} 个失败");
    }

    private HotKeyExecutionResult ToggleApplicationProjection()
    {
        var enabled = !_applicationSettings.Current.IsEnabled;
        _applicationSettings.UpdateGlobalSettings(
            enabled,
            _applicationSettings.Current.DefaultTargetDisplayId);
        return new HotKeyExecutionResult(true, enabled ? "自动投放已恢复" : "自动投放已暂停");
    }

    private HotKeyExecutionResult OpenSettings()
    {
        SettingsRequested?.Invoke(this, EventArgs.Empty);
        return new HotKeyExecutionResult(true, "设置已打开");
    }

    private static HotKeyExecutionResult Cursor(CursorSwitchResult result) => result switch
    {
        CursorSwitchResult.Moved => new HotKeyExecutionResult(true, "已切换"),
        CursorSwitchResult.NoTarget => new HotKeyExecutionResult(false, "没有可用目标屏幕"),
        _ => new HotKeyExecutionResult(false, "切换失败"),
    };

    private static HotKeyExecutionResult Window(WindowSwitchResult result) => result switch
    {
        WindowSwitchResult.Moved => new HotKeyExecutionResult(true, "已移动"),
        WindowSwitchResult.NoTarget => new HotKeyExecutionResult(false, "仅连接一块显示器"),
        WindowSwitchResult.NoWindow => new HotKeyExecutionResult(false, "当前窗口不可移动"),
        _ => new HotKeyExecutionResult(false, "移动失败"),
    };

    private static HotKeyExecutionResult Projection(WindowProjectionShortcutResult result) => result.Status switch
    {
        WindowProjectionShortcutStatus.Moved => new HotKeyExecutionResult(true, "已投放"),
        WindowProjectionShortcutStatus.NoWindow => new HotKeyExecutionResult(false, "当前窗口不可投放"),
        WindowProjectionShortcutStatus.TargetUnavailable => new HotKeyExecutionResult(false, "目标显示器未连接"),
        _ => new HotKeyExecutionResult(false, "投放失败"),
    };
}

public sealed record HotKeyExecutionResult(bool Succeeded, string StatusMessage);
