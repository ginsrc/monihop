using MoniHop.Core.ApplicationProjection;
using MoniHop.Core.Displays;
using MoniHop.Core.WindowProjection;
using MoniHop.Desktop.Settings;
using MoniHop.Windows.ApplicationProjection;
using MoniHop.Windows.Windows;

namespace MoniHop.Desktop.WindowProjection;

public sealed class WindowProjectionShortcutService
{
    private readonly IDisplayCatalog _displayCatalog;
    private readonly IWindowController _foregroundWindowController;
    private readonly IApplicationWindowController _windowController;
    private readonly WindowProjectionSettingsService _settingsService;
    private readonly WindowProjectionPlanner _planner;

    public WindowProjectionShortcutService(
        IDisplayCatalog displayCatalog,
        IWindowController foregroundWindowController,
        IApplicationWindowController windowController,
        WindowProjectionSettingsService settingsService,
        WindowProjectionPlanner? planner = null)
    {
        _displayCatalog = displayCatalog ?? throw new ArgumentNullException(nameof(displayCatalog));
        _foregroundWindowController = foregroundWindowController ?? throw new ArgumentNullException(nameof(foregroundWindowController));
        _windowController = windowController ?? throw new ArgumentNullException(nameof(windowController));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _planner = planner ?? new WindowProjectionPlanner();
    }

    public WindowProjectionCandidate? CaptureCurrent(nint excludedWindowHandle)
    {
        var handle = _foregroundWindowController.GetForegroundWindow();
        if (handle == 0 || handle == excludedWindowHandle)
        {
            return null;
        }

        var snapshot = _windowController.Read(handle);
        return snapshot is null ? null : new WindowProjectionCandidate(snapshot);
    }

    public WindowProjectionShortcutResult ProjectCurrent(
        nint excludedWindowHandle,
        ProjectionLayout? layout = null,
        string? targetDisplayId = null)
    {
        var candidate = CaptureCurrent(excludedWindowHandle);
        return candidate is null
            ? new WindowProjectionShortcutResult(WindowProjectionShortcutStatus.NoWindow)
            : Project(candidate, layout, targetDisplayId);
    }

    public WindowProjectionShortcutResult Project(
        WindowProjectionCandidate candidate,
        ProjectionLayout? layout = null,
        string? targetDisplayId = null)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var displays = _displayCatalog.ReadAll();
        if (displays.Count == 0)
        {
            return new WindowProjectionShortcutResult(WindowProjectionShortcutStatus.TargetUnavailable);
        }

        var sourceRect = candidate.Snapshot.Placement.IsMaximized
            ? candidate.Snapshot.Placement.NormalRect
            : candidate.Snapshot.Placement.WindowRect;
        var source = FindDisplay(displays, sourceRect);
        var target = ResolveTarget(displays, source, targetDisplayId, out var explicitTargetUnavailable);
        if (target is null || explicitTargetUnavailable)
        {
            return new WindowProjectionShortcutResult(WindowProjectionShortcutStatus.TargetUnavailable);
        }

        var effectiveLayout = ResolveSupportedLayout(
            layout ?? _settingsService.Current.DefaultLayout,
            candidate.Snapshot.Capabilities);
        var plan = _planner.Plan(displays, target, sourceRect, effectiveLayout);
        _windowController.Move(candidate.Snapshot.WindowHandle, new ApplicationProjectionPlan(
            plan.TargetDisplay,
            plan.TargetRect,
            plan.Layout,
            plan.ShouldMaximize,
            ProjectionRuleSource.Global,
            false));
        return new WindowProjectionShortcutResult(
            WindowProjectionShortcutStatus.Moved,
            plan.TargetDisplay.StableId,
            plan.Layout);
    }

    public IReadOnlyList<DisplaySnapshot> ReadDisplays() => _displayCatalog.ReadAll();

    public string? ResolveDefaultTargetId(WindowProjectionCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var displays = _displayCatalog.ReadAll();
        var sourceRect = candidate.Snapshot.Placement.IsMaximized
            ? candidate.Snapshot.Placement.NormalRect
            : candidate.Snapshot.Placement.WindowRect;
        return ResolveTarget(displays, FindDisplay(displays, sourceRect), null, out _)?.StableId;
    }

    private DisplaySnapshot? ResolveTarget(
        IReadOnlyList<DisplaySnapshot> displays,
        DisplaySnapshot? source,
        string? explicitTargetId,
        out bool explicitTargetUnavailable)
    {
        explicitTargetUnavailable = false;
        if (!string.IsNullOrWhiteSpace(explicitTargetId))
        {
            var explicitTarget = displays.FirstOrDefault(display =>
                StringComparer.OrdinalIgnoreCase.Equals(display.StableId, explicitTargetId));
            explicitTargetUnavailable = explicitTarget is null;
            return explicitTarget;
        }

        var configuredTargetId = _settingsService.Current.DefaultTargetDisplayId;
        var configured = configuredTargetId is null
            ? null
            : displays.FirstOrDefault(display =>
                StringComparer.OrdinalIgnoreCase.Equals(display.StableId, configuredTargetId));
        if (configured is not null)
        {
            return configured;
        }

        if (source is null || displays.Count < 2)
        {
            return null;
        }

        var sourceIndex = displays.ToList().FindIndex(display =>
            StringComparer.OrdinalIgnoreCase.Equals(display.StableId, source.StableId));
        return displays[(sourceIndex + 1) % displays.Count];
    }

    private static DisplaySnapshot? FindDisplay(
        IReadOnlyList<DisplaySnapshot> displays,
        PixelRect rect)
    {
        var center = new PixelPoint(rect.Left + (rect.Width / 2), rect.Top + (rect.Height / 2));
        return displays.FirstOrDefault(display => display.Bounds.Contains(center));
    }

    private static ProjectionLayout ResolveSupportedLayout(
        ProjectionLayout requested,
        WindowCapabilities capabilities) => requested switch
        {
            ProjectionLayout.Maximized when !capabilities.CanMaximize => ProjectionLayout.KeepSize,
            ProjectionLayout.LeftHalf or ProjectionLayout.RightHalf when !capabilities.CanResize => ProjectionLayout.KeepSize,
            _ => requested,
        };
}

public sealed record WindowProjectionCandidate(ApplicationWindowSnapshot Snapshot);

public sealed record WindowProjectionShortcutResult(
    WindowProjectionShortcutStatus Status,
    string? TargetDisplayId = null,
    ProjectionLayout? Layout = null);

public enum WindowProjectionShortcutStatus
{
    Moved,
    NoWindow,
    TargetUnavailable,
}
