using System.ComponentModel;
using MoniHop.Core.ApplicationProjection;
using MoniHop.Core.Displays;
using MoniHop.Core.WindowProjection;
using MoniHop.Desktop.Settings;
using MoniHop.Windows.ApplicationProjection;
using MoniHop.Windows.WindowProjection;
using MoniHop.Windows.Windows;

namespace MoniHop.Desktop.WindowProjection;

public sealed class WindowProjectionRuntime : IDisposable
{
    private static readonly PixelSize PortalSize = new(64, 28);
    private static readonly PixelSize PanelSize = new(420, 120);
    private readonly object _gate = new();
    private readonly IWindowMoveSizeEventSource _eventSource;
    private readonly IPointerState _pointer;
    private readonly IWindowProjectionOverlay _overlay;
    private readonly IApplicationWindowController _windowController;
    private readonly IDisplayCatalog _displayCatalog;
    private readonly WindowProjectionSettingsService _settingsService;
    private readonly DisplayProfileService? _displayProfileService;
    private readonly WindowProjectionPlanner _planner = new();
    private readonly TimeSpan _pollInterval;
    private CancellationTokenSource? _pollCancellation;
    private nint _draggedWindow;
    private IReadOnlyList<DisplaySnapshot> _displays = [];
    private DisplaySnapshot? _sourceDisplay;
    private DisplaySnapshot? _selectedTarget;
    private PixelRect? _sourceWindowRect;
    private WindowCapabilities _windowCapabilities = WindowCapabilities.Standard;
    private PixelRect _portalBounds;
    private WindowProjectionCommandLayout? _command;
    private WindowProjectionHit _lastHit = WindowProjectionHit.None;
    private OverlayState _state;
    private bool _disposed;

    public WindowProjectionRuntime(
        IWindowMoveSizeEventSource eventSource,
        IPointerState pointer,
        IWindowProjectionOverlay overlay,
        IApplicationWindowController windowController,
        IDisplayCatalog displayCatalog,
        WindowProjectionSettingsService settingsService,
        TimeSpan? pollInterval = null,
        DisplayProfileService? displayProfileService = null)
    {
        _eventSource = eventSource ?? throw new ArgumentNullException(nameof(eventSource));
        _pointer = pointer ?? throw new ArgumentNullException(nameof(pointer));
        _overlay = overlay ?? throw new ArgumentNullException(nameof(overlay));
        _windowController = windowController ?? throw new ArgumentNullException(nameof(windowController));
        _displayCatalog = displayCatalog ?? throw new ArgumentNullException(nameof(displayCatalog));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _displayProfileService = displayProfileService;
        _pollInterval = pollInterval ?? TimeSpan.FromMilliseconds(30);
        _eventSource.WindowChanged += OnWindowChanged;
        _settingsService.Changed += OnSettingsChanged;
    }

    public event EventHandler<WindowProjectionRuntimeResult>? ProjectionCompleted;

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _eventSource.WindowChanged -= OnWindowChanged;
            _settingsService.Changed -= OnSettingsChanged;
            StopPolling();
            ResetSession(hideOverlay: true);
        }

        _eventSource.Dispose();
        _overlay.Dispose();
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        lock (_gate)
        {
            if (!_settingsService.Current.IsEnabled)
            {
                StopPolling();
                ResetSession(hideOverlay: true);
            }
        }
    }

    private void OnWindowChanged(object? sender, WindowMoveSizeEvent value)
    {
        lock (_gate)
        {
            if (_disposed || !_settingsService.Current.IsEnabled)
            {
                return;
            }

            if (value.Kind == WindowMoveSizeEventKind.Started)
            {
                StartSession(value.WindowHandle);
                return;
            }

            if (value.Kind == WindowMoveSizeEventKind.Ended && value.WindowHandle == _draggedWindow)
            {
                EndSession(value.WindowHandle);
            }
        }
    }

    private void StartSession(nint windowHandle)
    {
        if (_draggedWindow != 0)
        {
            return;
        }

        var snapshot = _windowController.Read(windowHandle);
        if (snapshot is null)
        {
            return;
        }

        var sourceWindowRect = snapshot.Placement.IsMaximized
            ? snapshot.Placement.NormalRect
            : snapshot.Placement.WindowRect;
        if (sourceWindowRect.Width <= 0 || sourceWindowRect.Height <= 0)
        {
            return;
        }

        var point = ReadPointer();
        var liveDisplays = _displayCatalog.ReadAll();
        _displays = _displayProfileService?.ApplyNames(liveDisplays) ?? liveDisplays;
        if (_displays.Count < 2)
        {
            return;
        }

        _sourceDisplay = _displays.FirstOrDefault(display => display.WorkingArea.Contains(point)) ??
            _displays.FirstOrDefault(display => display.IsPrimary) ??
            _displays[0];
        _selectedTarget = ResolveDefaultTarget(_displays, _sourceDisplay, _settingsService.Current.DefaultTargetDisplayId);
        _sourceWindowRect = sourceWindowRect;
        _windowCapabilities = snapshot.Capabilities;
        _draggedWindow = windowHandle;
        ShowPortal();
        StartPolling();
    }

    private void EndSession(nint windowHandle)
    {
        var hit = _command is null
            ? WindowProjectionHit.None
            : _planner.HitTest(_command, ReadPointer());
        var target = _selectedTarget;
        var displays = _displays;
        var sourceWindowRect = _sourceWindowRect;
        var requestedLayout = hit.Kind switch
        {
            WindowProjectionHitKind.DefaultDrop => _settingsService.Current.DefaultLayout,
            WindowProjectionHitKind.Layout => hit.Layout,
            _ => null,
        };
        ProjectionLayout? layout = requestedLayout is null
            ? null
            : ResolveSupportedLayout(requestedLayout.Value, _windowCapabilities);

        StopPolling();
        ResetSession(hideOverlay: true);
        if (target is null || sourceWindowRect is null || layout is null || _windowController.Read(windowHandle) is null)
        {
            return;
        }

        try
        {
            var plan = _planner.Plan(displays, target, sourceWindowRect.Value, layout.Value);
            _windowController.Move(windowHandle, new ApplicationProjectionPlan(
                plan.TargetDisplay,
                plan.TargetRect,
                plan.Layout,
                plan.ShouldMaximize,
                ProjectionRuleSource.Global,
                false));
            ProjectionCompleted?.Invoke(this, new WindowProjectionRuntimeResult(
                WindowProjectionRuntimeStatus.Moved,
                plan.TargetDisplay.StableId,
                plan.Layout,
                null));
        }
        catch (Exception exception) when (exception is Win32Exception or UnauthorizedAccessException or ArgumentException)
        {
            ProjectionCompleted?.Invoke(this, new WindowProjectionRuntimeResult(
                WindowProjectionRuntimeStatus.Failed,
                null,
                null,
                exception.Message));
        }
    }

    private void StartPolling()
    {
        StopPolling();
        _pollCancellation = new CancellationTokenSource();
        _ = PollAsync(_pollCancellation.Token);
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        DateTime? portalEnteredAt = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                lock (_gate)
                {
                    if (_draggedWindow == 0)
                    {
                        return;
                    }

                    if (_windowController.Read(_draggedWindow) is null)
                    {
                        AbortSession();
                        return;
                    }

                    var point = ReadPointer();
                    if (_state == OverlayState.PortalVisible)
                    {
                        if (_portalBounds.Contains(point))
                        {
                            portalEnteredAt ??= DateTime.UtcNow;
                            if (_settingsService.Current.TriggerMode == WindowProjectionTriggerMode.Immediate ||
                                DateTime.UtcNow - portalEnteredAt >= TimeSpan.FromMilliseconds(300))
                            {
                                ShowCommands();
                            }
                        }
                        else
                        {
                            portalEnteredAt = null;
                        }
                    }
                    else if (_state == OverlayState.CommandsVisible && _command is not null)
                    {
                        UpdateCommandHit(point);
                    }
                }

                await Task.Delay(_pollInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Win32Exception)
            {
                lock (_gate)
                {
                    AbortSession();
                }

                return;
            }
        }
    }

    private void ShowPortal()
    {
        if (_state != OverlayState.Hidden || _sourceDisplay is null || _selectedTarget is null)
        {
            return;
        }

        _portalBounds = _planner.CalculatePortalBounds(
            _sourceDisplay.Bounds,
            PortalSize,
            _settingsService.Current.RelativePosition);
        _overlay.ShowPortal(_portalBounds);
        _state = OverlayState.PortalVisible;
    }

    private void ShowCommands()
    {
        if (_state != OverlayState.PortalVisible || _sourceDisplay is null || _selectedTarget is null)
        {
            return;
        }

        var panelBounds = _planner.CalculateExpandedBounds(
            _sourceDisplay.Bounds,
            PanelSize,
            _portalBounds);
        _command = _planner.CreateCommandLayout(_portalBounds, panelBounds, _displays, _selectedTarget);
        _lastHit = _planner.HitTest(_command, ReadPointer());
        _overlay.ShowCommands(_command, _settingsService.Current.DefaultLayout, _lastHit);
        _state = OverlayState.CommandsVisible;
    }

    private void UpdateCommandHit(PixelPoint point)
    {
        if (_command is null || _selectedTarget is null)
        {
            return;
        }

        var hit = _planner.HitTest(_command, point);
        if (hit.Kind == WindowProjectionHitKind.Display && hit.TargetDisplay is not null &&
            !StringComparer.OrdinalIgnoreCase.Equals(hit.TargetDisplay.StableId, _selectedTarget.StableId))
        {
            _selectedTarget = hit.TargetDisplay;
            _command = _planner.CreateCommandLayout(
                _command.PortalBounds,
                _command.PanelBounds,
                _displays,
                _selectedTarget);
            hit = _planner.HitTest(_command, point);
        }

        if (hit == _lastHit)
        {
            return;
        }

        _lastHit = hit;
        _overlay.UpdateCommands(_command, _settingsService.Current.DefaultLayout, hit);
    }

    private static DisplaySnapshot ResolveDefaultTarget(
        IReadOnlyList<DisplaySnapshot> displays,
        DisplaySnapshot sourceDisplay,
        string? configuredTargetId)
    {
        var configured = configuredTargetId is null
            ? null
            : displays.FirstOrDefault(display => StringComparer.OrdinalIgnoreCase.Equals(display.StableId, configuredTargetId));
        if (configured is not null)
        {
            return configured;
        }

        var sourceIndex = -1;
        for (var index = 0; index < displays.Count; index++)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(displays[index].StableId, sourceDisplay.StableId))
            {
                sourceIndex = index;
                break;
            }
        }

        return displays[(sourceIndex + 1) % displays.Count];
    }

    private PixelPoint ReadPointer() => _pointer.ReadPosition();

    private void ResetSession(bool hideOverlay)
    {
        if (hideOverlay && _state != OverlayState.Hidden)
        {
            _overlay.Hide();
        }

        _draggedWindow = 0;
        _displays = [];
        _sourceDisplay = null;
        _selectedTarget = null;
        _sourceWindowRect = null;
        _windowCapabilities = WindowCapabilities.Standard;
        _portalBounds = default;
        _command = null;
        _lastHit = WindowProjectionHit.None;
        _state = OverlayState.Hidden;
    }

    private void AbortSession()
    {
        StopPolling();
        ResetSession(hideOverlay: true);
    }

    private void StopPolling()
    {
        _pollCancellation?.Cancel();
        _pollCancellation?.Dispose();
        _pollCancellation = null;
    }

    private enum OverlayState
    {
        Hidden,
        PortalVisible,
        CommandsVisible,
    }

    private static ProjectionLayout ResolveSupportedLayout(
        ProjectionLayout requested,
        WindowCapabilities capabilities) =>
        requested switch
        {
            ProjectionLayout.Maximized when !capabilities.CanMaximize => ProjectionLayout.KeepSize,
            ProjectionLayout.LeftHalf or ProjectionLayout.RightHalf when !capabilities.CanResize =>
                ProjectionLayout.KeepSize,
            _ => requested,
        };
}

public sealed record WindowProjectionRuntimeResult(
    WindowProjectionRuntimeStatus Status,
    string? TargetDisplayId,
    ProjectionLayout? Layout,
    string? ErrorMessage);

public enum WindowProjectionRuntimeStatus
{
    Moved,
    Failed,
}
