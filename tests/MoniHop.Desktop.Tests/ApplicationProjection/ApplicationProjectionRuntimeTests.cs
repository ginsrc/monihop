using MoniHop.Core.ApplicationProjection;
using MoniHop.Core.Displays;
using MoniHop.Desktop.ApplicationProjection;
using MoniHop.Desktop.Settings;
using MoniHop.Windows.ApplicationProjection;
using MoniHop.Windows.Windows;
using ProjectionApplicationIdentity = MoniHop.Core.ApplicationProjection.ApplicationIdentity;

namespace MoniHop.Desktop.Tests.ApplicationProjection;

public sealed class ApplicationProjectionRuntimeTests
{
    [Fact]
    public async Task ShownEvent_ProjectsEachWindowHandleOnlyOnce()
    {
        var source = new StubEventSource();
        var controller = new RecordingController(Snapshot(42));
        using var runtime = Runtime(source, controller, isEnabled: true);

        source.Raise(new WindowEvent(WindowEventKind.Shown, 42));
        source.Raise(new WindowEvent(WindowEventKind.Shown, 42));
        await controller.Moved.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(1, controller.MoveCount);
    }

    [Fact]
    public async Task DestroyedEvent_AllowsReusedHandleToBeProjectedAgain()
    {
        var source = new StubEventSource();
        var controller = new RecordingController(Snapshot(42));
        using var runtime = Runtime(source, controller, isEnabled: true);

        source.Raise(new WindowEvent(WindowEventKind.Shown, 42));
        await controller.WaitForMoveCount(1);
        source.Raise(new WindowEvent(WindowEventKind.Destroyed, 42));
        source.Raise(new WindowEvent(WindowEventKind.Shown, 42));
        await controller.WaitForMoveCount(2);

        Assert.Equal(2, controller.MoveCount);
    }

    [Fact]
    public async Task ShownEvent_DoesNothingWhenFeatureIsDisabled()
    {
        var source = new StubEventSource();
        var controller = new RecordingController(Snapshot(42));
        using var runtime = Runtime(source, controller, isEnabled: false);

        source.Raise(new WindowEvent(WindowEventKind.Shown, 42));
        await Task.Delay(50);

        Assert.Equal(0, controller.MoveCount);
    }

    [Fact]
    public async Task ShownEvent_RetriesWindowReadButMovesOnlyOnce()
    {
        var source = new StubEventSource();
        var controller = new RecordingController(Snapshot(42)) { MissingReadCount = 2 };
        using var runtime = Runtime(source, controller, isEnabled: true, retryDelay: TimeSpan.Zero);

        source.Raise(new WindowEvent(WindowEventKind.Shown, 42));
        await controller.Moved.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(3, controller.ReadCount);
        Assert.Equal(1, controller.MoveCount);
    }

    [Fact]
    public async Task ShownEvent_DefaultTimingWaitsForSettleDelayBeforeReading()
    {
        var source = new StubEventSource();
        var controller = new RecordingController(Snapshot(42));
        var store = new MemoryStore(new ApplicationProjectionSettings(true, "stable-b", []));
        using var runtime = new ApplicationProjectionRuntime(
            source,
            controller,
            new StubDisplayCatalog(),
            new ApplicationProjectionSettingsService(store));

        source.Raise(new WindowEvent(WindowEventKind.Shown, 42));
        await Task.Delay(50);

        // 尚未到达默认 200ms 窗口稳定去抖，不应开始读取。
        Assert.Equal(0, controller.ReadCount);

        await Task.Delay(300);
        Assert.True(controller.ReadCount > 0);
    }

    [Fact]
    public async Task ShownEvent_UsesRestorableRectForMaximizedWindow()
    {
        var source = new StubEventSource();
        var snapshot = Snapshot(42) with
        {
            Placement = new WindowPlacementSnapshot(
                3,
                new PixelRect(0, 0, 100, 100),
                new PixelRect(10, 10, 70, 70)),
        };
        var controller = new RecordingController(snapshot);
        using var runtime = Runtime(source, controller, isEnabled: true);

        source.Raise(new WindowEvent(WindowEventKind.Shown, 42));
        await controller.Moved.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(new PixelRect(110, 10, 170, 70), controller.WrittenPlan?.TargetRect);
    }

    [Fact]
    public async Task ShownEvent_FallsBackToKeepSizeWhenWindowCannotResize()
    {
        var source = new StubEventSource();
        var snapshot = Snapshot(42) with
        {
            Capabilities = new WindowCapabilities(false, false),
        };
        var controller = new RecordingController(snapshot);
        using var runtime = Runtime(
            source,
            controller,
            isEnabled: true,
            ruleLayout: ProjectionLayout.Maximized);

        source.Raise(new WindowEvent(WindowEventKind.Shown, 42));
        await controller.Moved.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(ProjectionLayout.KeepSize, controller.WrittenPlan?.Layout);
        Assert.Equal(new PixelRect(110, 10, 170, 70), controller.WrittenPlan?.TargetRect);
        Assert.False(controller.WrittenPlan?.ShouldMaximize);
    }

    [Fact]
    public async Task DestroyedEvent_DoesNotLetCanceledWorkRemoveReusedHandleWork()
    {
        var source = new StubEventSource();
        var controller = new RecordingController(Snapshot(42));
        using var runtime = Runtime(
            source,
            controller,
            isEnabled: true,
            settleDelay: TimeSpan.FromMilliseconds(50),
            retryDelay: TimeSpan.Zero);

        source.Raise(new WindowEvent(WindowEventKind.Shown, 42));
        source.Raise(new WindowEvent(WindowEventKind.Destroyed, 42));
        source.Raise(new WindowEvent(WindowEventKind.Shown, 42));
        await controller.Moved.Task.WaitAsync(TimeSpan.FromSeconds(2));
        source.Raise(new WindowEvent(WindowEventKind.Shown, 42));
        await Task.Delay(100);

        Assert.Equal(1, controller.MoveCount);
    }

    [Fact]
    public async Task ShownEvent_ReportsFailureOnceWhenWindowCannotBeMoved()
    {
        var source = new StubEventSource();
        var controller = new RecordingController(Snapshot(42))
        {
            MoveException = new ArgumentException("Window disappeared."),
        };
        using var runtime = Runtime(source, controller, isEnabled: true);
        var completed = new TaskCompletionSource<ApplicationProjectionRuntimeResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        runtime.ProjectionCompleted += (_, result) => completed.TrySetResult(result);

        source.Raise(new WindowEvent(WindowEventKind.Shown, 42));
        var result = await completed.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(ApplicationProjectionRuntimeStatus.Failed, result.Status);
        Assert.Equal(1, controller.MoveCount);
    }

    private static ApplicationProjectionRuntime Runtime(
        StubEventSource source,
        RecordingController controller,
        bool isEnabled,
        TimeSpan? retryDelay = null,
        TimeSpan? settleDelay = null,
        ProjectionLayout? ruleLayout = null)
    {
        var rules = ruleLayout is null
            ? []
            : new[]
            {
                new ApplicationProjectionRule(
                    controller.Snapshot.Application,
                    controller.Snapshot.DisplayName,
                    "stable-b",
                    ruleLayout.Value,
                    true),
            };
        var store = new MemoryStore(new ApplicationProjectionSettings(isEnabled, "stable-b", rules));
        return new ApplicationProjectionRuntime(
            source,
            controller,
            new StubDisplayCatalog(),
            new ApplicationProjectionSettingsService(store),
            settleDelay ?? TimeSpan.Zero,
            retryDelay ?? TimeSpan.FromMilliseconds(10));
    }

    private static ApplicationWindowSnapshot Snapshot(nint handle) =>
        new(
            handle,
            new ProjectionApplicationIdentity(ApplicationIdentityKind.ExecutablePath, @"C:\Apps\browser.exe"),
            "Browser",
            new WindowPlacementSnapshot(
                1,
                new PixelRect(10, 10, 70, 70),
                new PixelRect(10, 10, 70, 70)));

    private sealed class StubEventSource : IWindowEventSource
    {
        public event EventHandler<WindowEvent>? WindowChanged;
        public void Raise(WindowEvent value) => WindowChanged?.Invoke(this, value);
        public void Dispose() { }
    }

    private sealed class RecordingController(ApplicationWindowSnapshot snapshot) : IApplicationWindowController
    {
        public ApplicationWindowSnapshot Snapshot { get; } = snapshot;
        public TaskCompletionSource Moved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int MissingReadCount { get; init; }
        public int ReadCount { get; private set; }
        public int MoveCount { get; private set; }
        public ApplicationProjectionPlan? WrittenPlan { get; private set; }
        public Exception? MoveException { get; init; }
        public ApplicationWindowSnapshot? Read(nint windowHandle)
        {
            ReadCount++;
            return ReadCount <= MissingReadCount ? null : Snapshot;
        }
        public IReadOnlyList<ApplicationWindowSnapshot> ReadAll() => [Snapshot];
        public void Move(nint windowHandle, ApplicationProjectionPlan plan)
        {
            MoveCount++;
            WrittenPlan = plan;
            if (MoveException is not null)
            {
                throw MoveException;
            }

            Moved.TrySetResult();
        }

        public async Task WaitForMoveCount(int count)
        {
            var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(2);
            while (MoveCount < count && DateTime.UtcNow < timeout)
            {
                await Task.Delay(10);
            }

            Assert.Equal(count, MoveCount);
        }
    }

    private sealed class StubDisplayCatalog : IDisplayCatalog
    {
        public IReadOnlyList<DisplaySnapshot> ReadAll() =>
        [
            new("DISPLAY1", "Display 1", new PixelRect(0, 0, 100, 100), new PixelRect(0, 0, 100, 100), true, "stable-a"),
            new("DISPLAY2", "Display 2", new PixelRect(100, 0, 200, 100), new PixelRect(100, 0, 200, 100), false, "stable-b"),
        ];
    }

    private sealed class MemoryStore(ApplicationProjectionSettings settings) : IApplicationProjectionStore
    {
        public ApplicationProjectionSettings Load() => settings;
        public void Save(ApplicationProjectionSettings value) => settings = value;
    }
}
