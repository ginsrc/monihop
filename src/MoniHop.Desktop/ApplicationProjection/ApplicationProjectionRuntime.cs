using System.ComponentModel;
using MoniHop.Core.ApplicationProjection;
using MoniHop.Core.Displays;
using MoniHop.Desktop.Settings;
using MoniHop.Windows.ApplicationProjection;
using MoniHop.Windows.Windows;

namespace MoniHop.Desktop.ApplicationProjection;

public sealed class ApplicationProjectionRuntime : IDisposable
{
    private readonly object _gate = new();
    private readonly IWindowEventSource _eventSource;
    private readonly IApplicationWindowController _windowController;
    private readonly IDisplayCatalog _displayCatalog;
    private readonly ApplicationProjectionSettingsService _settingsService;
    private readonly ApplicationProjectionPlanner _planner = new();
    private readonly TimeSpan _settleDelay;
    private readonly TimeSpan _retryDelay;
    private readonly Dictionary<nint, CancellationTokenSource> _pending = [];
    private readonly HashSet<nint> _attempted = [];
    private bool _disposed;

    public ApplicationProjectionRuntime(
        IWindowEventSource eventSource,
        IApplicationWindowController windowController,
        IDisplayCatalog displayCatalog,
        ApplicationProjectionSettingsService settingsService,
        TimeSpan? settleDelay = null,
        TimeSpan? retryDelay = null)
    {
        _eventSource = eventSource ?? throw new ArgumentNullException(nameof(eventSource));
        _windowController = windowController ?? throw new ArgumentNullException(nameof(windowController));
        _displayCatalog = displayCatalog ?? throw new ArgumentNullException(nameof(displayCatalog));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _settleDelay = settleDelay ?? TimeSpan.Zero;
        _retryDelay = retryDelay ?? TimeSpan.FromMilliseconds(120);
        _eventSource.WindowChanged += OnWindowChanged;
    }

    public event EventHandler<ApplicationProjectionRuntimeResult>? ProjectionCompleted;

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
            foreach (var cancellation in _pending.Values)
            {
                cancellation.Cancel();
                cancellation.Dispose();
            }

            _pending.Clear();
        }

        _eventSource.Dispose();
    }

    private void OnWindowChanged(object? sender, WindowEvent windowEvent)
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            if (windowEvent.Kind == WindowEventKind.Destroyed)
            {
                if (_pending.Remove(windowEvent.WindowHandle, out var cancellation))
                {
                    cancellation.Cancel();
                    cancellation.Dispose();
                }

                _attempted.Remove(windowEvent.WindowHandle);
                return;
            }

            if (!_settingsService.Current.IsEnabled ||
                _attempted.Contains(windowEvent.WindowHandle) ||
                _pending.ContainsKey(windowEvent.WindowHandle))
            {
                return;
            }

            var pending = new CancellationTokenSource();
            _pending.Add(windowEvent.WindowHandle, pending);
            _ = ProcessAsync(windowEvent.WindowHandle, pending);
        }
    }

    private async Task ProcessAsync(nint windowHandle, CancellationTokenSource pending)
    {
        try
        {
            await Task.Delay(_settleDelay, pending.Token).ConfigureAwait(false);
            lock (_gate)
            {
                if (_disposed || pending.IsCancellationRequested)
                {
                    return;
                }

                _pending.Remove(windowHandle);
                _attempted.Add(windowHandle);
            }

            var window = await ReadWindowAsync(windowHandle, pending.Token).ConfigureAwait(false);
            if (window is null)
            {
                return;
            }

            var displays = _displayCatalog.ReadAll();
            var sourceRect = window.Placement.IsMaximized
                ? window.Placement.NormalRect
                : window.Placement.WindowRect;
            var plan = _planner.Plan(
                _settingsService.Current,
                window.Application,
                displays,
                sourceRect);
            if (plan is null)
            {
                return;
            }

            var effectiveLayout = ResolveSupportedLayout(plan.Layout, window.Capabilities);
            if (effectiveLayout != plan.Layout)
            {
                plan = new ApplicationProjectionPlan(
                    plan.TargetDisplay,
                    ProjectionGeometry.CalculateTargetRect(
                        displays,
                        plan.TargetDisplay,
                        sourceRect,
                        effectiveLayout),
                    effectiveLayout,
                    false,
                    plan.RuleSource,
                    plan.UsedPrimaryFallback);
            }

            _windowController.Move(windowHandle, plan);
            ProjectionCompleted?.Invoke(
                this,
                new ApplicationProjectionRuntimeResult(
                    ApplicationProjectionRuntimeStatus.Moved,
                    window.DisplayName,
                    plan.TargetDisplay.StableId,
                    plan.UsedPrimaryFallback,
                    null));
        }
        catch (OperationCanceledException) when (pending.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (
            exception is Win32Exception or UnauthorizedAccessException or ArgumentException)
        {
            ProjectionCompleted?.Invoke(
                this,
                new ApplicationProjectionRuntimeResult(
                    ApplicationProjectionRuntimeStatus.Failed,
                    null,
                    null,
                    false,
                    exception.Message));
        }
        finally
        {
            lock (_gate)
            {
                if (_pending.TryGetValue(windowHandle, out var currentPending) &&
                    ReferenceEquals(currentPending, pending))
                {
                    _pending.Remove(windowHandle);
                }
            }

            pending.Dispose();
        }
    }

    private async Task<ApplicationWindowSnapshot?> ReadWindowAsync(
        nint windowHandle,
        CancellationToken cancellationToken)
    {
        const int maximumAttempts = 3;
        for (var attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            var window = _windowController.Read(windowHandle);
            if (window is not null)
            {
                return window;
            }

            if (attempt < maximumAttempts)
            {
                await Task.Delay(_retryDelay, cancellationToken).ConfigureAwait(false);
            }
        }

        return null;
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

public sealed record ApplicationProjectionRuntimeResult(
    ApplicationProjectionRuntimeStatus Status,
    string? ApplicationName,
    string? TargetDisplayId,
    bool UsedPrimaryFallback,
    string? ErrorMessage);

public enum ApplicationProjectionRuntimeStatus
{
    Moved,
    Failed,
}
