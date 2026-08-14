using MoniHop.Core.ApplicationProjection;
using MoniHop.Core.Displays;
using MoniHop.Core.WindowProjection;
using MoniHop.Desktop.Settings;
using MoniHop.Desktop.WindowProjection;
using MoniHop.Windows.ApplicationProjection;
using MoniHop.Windows.WindowProjection;
using MoniHop.Windows.Windows;
using ProjectionApplicationIdentity = MoniHop.Core.ApplicationProjection.ApplicationIdentity;

namespace MoniHop.Desktop.Tests.WindowProjection;

public sealed class WindowProjectionRuntimeTests
{
    [Fact]
    public async Task DisabledFeature_DoesNotShowOverlayOrMoveWindow()
    {
        var source = new FakeMoveSizeSource();
        var overlay = new FakeOverlay();
        var controller = new FakeWindowController();
        using var runtime = Create(source, overlay, controller, WindowProjectionSettings.Default);

        source.Raise(WindowMoveSizeEventKind.Started, 42);
        source.Raise(WindowMoveSizeEventKind.Ended, 42);
        await Task.Delay(40);

        Assert.Equal(0, overlay.ShowCount);
        Assert.Equal(0, controller.MoveCount);
    }

    [Fact]
    public void ImmediateMode_ShowsCompactPortalAtMoveStart()
    {
        var source = new FakeMoveSizeSource();
        var overlay = new FakeOverlay();
        using var runtime = Create(source, overlay, new FakeWindowController(),
            Settings());

        source.Raise(WindowMoveSizeEventKind.Started, 42);

        Assert.Equal(1, overlay.PortalShowCount);
        Assert.Equal(0, overlay.CommandShowCount);
        Assert.Equal(new PixelRect(468, 0, 532, 28), overlay.PortalBounds);
    }

    [Fact]
    public async Task EdgeDwellMode_ShowsPortalAtMoveStartAndExpandsAfterPortalDwell()
    {
        var source = new FakeMoveSizeSource();
        var pointer = new FakePointerState(new PixelPoint(300, 50));
        var overlay = new FakeOverlay();
        using var runtime = Create(source, overlay, new FakeWindowController(),
            Settings(triggerMode: WindowProjectionTriggerMode.EdgeDwell), pointer);

        source.Raise(WindowMoveSizeEventKind.Started, 42);
        Assert.Equal(1, overlay.PortalShowCount);
        Assert.Equal(0, overlay.CommandShowCount);

        pointer.Position = new PixelPoint(500, 10);
        await Task.Delay(120);
        Assert.Equal(1, overlay.PortalShowCount);
        Assert.Equal(0, overlay.CommandShowCount);
        await WaitFor(() => overlay.CommandShowCount == 1);
    }

    [Fact]
    public async Task EnteringPortal_ExpandsCompactCommandPanel()
    {
        var source = new FakeMoveSizeSource();
        var pointer = new FakePointerState(new PixelPoint(50, 50));
        var overlay = new FakeOverlay();
        using var runtime = Create(source, overlay, new FakeWindowController(), Settings(), pointer);

        source.Raise(WindowMoveSizeEventKind.Started, 42);
        pointer.Position = new PixelPoint(500, 10);
        await WaitFor(() => overlay.CommandShowCount == 1);

        Assert.NotNull(overlay.Command);
        Assert.Equal(new PixelRect(290, 28, 710, 148), overlay.Command.PanelBounds);
        Assert.Equal("stable-b", Assert.Single(overlay.Command.DisplayZones, zone => zone.IsSelected).TargetDisplay.StableId);
        Assert.Equal(ProjectionLayout.Maximized, overlay.DefaultLayout);
    }

    [Fact]
    public async Task EndedEvent_InDefaultDropMovesToConfiguredDefaultTargetAndLayout()
    {
        var source = new FakeMoveSizeSource();
        var pointer = new FakePointerState(new PixelPoint(50, 50));
        var overlay = new FakeOverlay();
        var controller = new FakeWindowController();
        using var runtime = Create(source, overlay, controller, Settings(defaultLayout: ProjectionLayout.RightHalf), pointer);

        source.Raise(WindowMoveSizeEventKind.Started, 42);
        pointer.Position = new PixelPoint(500, 10);
        await WaitFor(() => overlay.CommandShowCount == 1);
        pointer.Position = new PixelPoint(400, 60);
        source.Raise(WindowMoveSizeEventKind.Ended, 42);
        await WaitFor(() => controller.MoveCount == 1);

        Assert.Equal(1, overlay.HideCount);
        Assert.Equal("stable-b", controller.Plan?.TargetDisplay.StableId);
        Assert.Equal(ProjectionLayout.RightHalf, controller.Plan?.Layout);
        Assert.Equal((nint)42, controller.LastHandle);
    }

    [Fact]
    public async Task KeepSizeDefault_UsesDragStartPlacementInsteadOfReleasePlacement()
    {
        var source = new FakeMoveSizeSource();
        var pointer = new FakePointerState(new PixelPoint(50, 50));
        var overlay = new FakeOverlay();
        var controller = new FakeWindowController
        {
            InitialPlacement = new WindowPlacementSnapshot(
                1,
                new PixelRect(120, 40, 320, 180),
                new PixelRect(120, 40, 320, 180)),
            ReleasePlacement = new WindowPlacementSnapshot(
                1,
                new PixelRect(450, 0, 650, 140),
                new PixelRect(450, 0, 650, 140)),
        };
        using var runtime = Create(source, overlay, controller, Settings(defaultLayout: ProjectionLayout.KeepSize), pointer);

        source.Raise(WindowMoveSizeEventKind.Started, 42);
        pointer.Position = new PixelPoint(500, 10);
        await WaitFor(() => overlay.CommandShowCount == 1);
        pointer.Position = new PixelPoint(400, 60);
        source.Raise(WindowMoveSizeEventKind.Ended, 42);
        await WaitFor(() => controller.MoveCount == 1);

        Assert.Equal(ProjectionLayout.KeepSize, controller.Plan?.Layout);
        Assert.Equal(new PixelRect(1120, 40, 1320, 180), controller.Plan?.TargetRect);
    }

    [Fact]
    public async Task KeepSizeDefault_UsesRestorableRectWhenDragStartsMaximized()
    {
        var source = new FakeMoveSizeSource();
        var pointer = new FakePointerState(new PixelPoint(50, 50));
        var overlay = new FakeOverlay();
        var controller = new FakeWindowController
        {
            InitialPlacement = new WindowPlacementSnapshot(
                3,
                new PixelRect(0, 0, 1000, 800),
                new PixelRect(200, 100, 600, 400)),
        };
        using var runtime = Create(source, overlay, controller, Settings(defaultLayout: ProjectionLayout.KeepSize), pointer);

        source.Raise(WindowMoveSizeEventKind.Started, 42);
        pointer.Position = new PixelPoint(500, 10);
        await WaitFor(() => overlay.CommandShowCount == 1);
        pointer.Position = new PixelPoint(400, 60);
        source.Raise(WindowMoveSizeEventKind.Ended, 42);
        await WaitFor(() => controller.MoveCount == 1);

        Assert.Equal(new PixelRect(1200, 100, 1600, 400), controller.Plan?.TargetRect);
    }

    [Fact]
    public async Task MaximizedDefault_FallsBackToKeepSizeForFixedSizeWindow()
    {
        var source = new FakeMoveSizeSource();
        var pointer = new FakePointerState(new PixelPoint(50, 50));
        var overlay = new FakeOverlay();
        var controller = new FakeWindowController
        {
            Capabilities = new WindowCapabilities(false, false),
        };
        using var runtime = Create(source, overlay, controller, Settings(), pointer);

        source.Raise(WindowMoveSizeEventKind.Started, 42);
        pointer.Position = new PixelPoint(500, 10);
        await WaitFor(() => overlay.CommandShowCount == 1);
        pointer.Position = new PixelPoint(400, 60);
        source.Raise(WindowMoveSizeEventKind.Ended, 42);
        await WaitFor(() => controller.MoveCount == 1);

        Assert.Equal(ProjectionLayout.KeepSize, controller.Plan?.Layout);
        Assert.Equal(new PixelRect(1010, 10, 1070, 70), controller.Plan?.TargetRect);
        Assert.False(controller.Plan?.ShouldMaximize);
    }

    [Fact]
    public async Task MoveStart_UsesCustomDisplayNamesInCommandTargets()
    {
        var source = new FakeMoveSizeSource();
        var pointer = new FakePointerState(new PixelPoint(50, 50));
        var overlay = new FakeOverlay();
        var displayCatalog = new FakeDisplayCatalog();
        var profiles = new DisplayProfileService(displayCatalog, new MemoryDisplayProfileStore());
        profiles.Refresh();
        profiles.Rename("stable-b", "Work screen");
        using var runtime = Create(
            source,
            overlay,
            new FakeWindowController(),
            Settings(),
            pointer,
            displayCatalog: displayCatalog,
            displayProfileService: profiles);

        source.Raise(WindowMoveSizeEventKind.Started, 42);
        pointer.Position = new PixelPoint(500, 10);
        await WaitFor(() => overlay.CommandShowCount == 1);

        Assert.Equal(
            "Work screen",
            Assert.Single(
                overlay.Command?.DisplayZones ?? [],
                zone => zone.TargetDisplay.StableId == "stable-b").TargetDisplay.DisplayName);
    }

    [Fact]
    public async Task DisplaySelectionChangesThisDropTargetAndLayoutZoneOverridesDefaultLayout()
    {
        var source = new FakeMoveSizeSource();
        var pointer = new FakePointerState(new PixelPoint(50, 50));
        var overlay = new FakeOverlay();
        var controller = new FakeWindowController();
        using var runtime = Create(source, overlay, controller, Settings(defaultLayout: ProjectionLayout.RightHalf), pointer);

        source.Raise(WindowMoveSizeEventKind.Started, 42);
        pointer.Position = new PixelPoint(500, 10);
        await WaitFor(() => overlay.CommandShowCount == 1);
        pointer.Position = new PixelPoint(650, 60);
        await WaitFor(() => overlay.CommandUpdateCount > 0 && overlay.SelectedTargetId == "stable-a");
        pointer.Position = new PixelPoint(450, 120);
        source.Raise(WindowMoveSizeEventKind.Ended, 42);
        await WaitFor(() => controller.MoveCount == 1);

        Assert.Equal("stable-a", controller.Plan?.TargetDisplay.StableId);
        Assert.Equal(ProjectionLayout.LeftHalf, controller.Plan?.Layout);
    }

    [Fact]
    public async Task MissingConfiguredTargetFallsBackToNextDisplayWithoutChangingSettings()
    {
        var source = new FakeMoveSizeSource();
        var pointer = new FakePointerState(new PixelPoint(50, 50));
        var overlay = new FakeOverlay();
        var controller = new FakeWindowController();
        var settings = Settings(defaultTargetDisplayId: "missing");
        using var runtime = Create(source, overlay, controller, settings, pointer);

        source.Raise(WindowMoveSizeEventKind.Started, 42);
        pointer.Position = new PixelPoint(500, 10);
        await WaitFor(() => overlay.CommandShowCount == 1);
        pointer.Position = new PixelPoint(400, 60);
        source.Raise(WindowMoveSizeEventKind.Ended, 42);
        await WaitFor(() => controller.MoveCount == 1);

        Assert.Equal("stable-b", controller.Plan?.TargetDisplay.StableId);
        Assert.Equal("missing", settings.DefaultTargetDisplayId);
    }

    [Fact]
    public async Task EndedEvent_OutsideCommandPanelDoesNotMoveWindow()
    {
        var source = new FakeMoveSizeSource();
        var pointer = new FakePointerState(new PixelPoint(50, 50));
        var overlay = new FakeOverlay();
        var controller = new FakeWindowController();
        using var runtime = Create(source, overlay, controller, Settings(), pointer);

        source.Raise(WindowMoveSizeEventKind.Started, 42);
        pointer.Position = new PixelPoint(500, 10);
        await WaitFor(() => overlay.CommandShowCount == 1);
        pointer.Position = new PixelPoint(800, 500);
        source.Raise(WindowMoveSizeEventKind.Ended, 42);
        await Task.Delay(40);

        Assert.Equal(0, controller.MoveCount);
        Assert.Equal(1, overlay.HideCount);
    }

    [Fact]
    public async Task MoveFailurePublishesFailedResultAndLeavesWindowUnchanged()
    {
        var source = new FakeMoveSizeSource();
        var pointer = new FakePointerState(new PixelPoint(50, 50));
        var controller = new FakeWindowController { MoveError = new UnauthorizedAccessException("denied") };
        using var runtime = Create(source, new FakeOverlay(), controller, Settings(), pointer);
        WindowProjectionRuntimeResult? result = null;
        runtime.ProjectionCompleted += (_, value) => result = value;

        source.Raise(WindowMoveSizeEventKind.Started, 42);
        pointer.Position = new PixelPoint(500, 10);
        await Task.Delay(40);
        pointer.Position = new PixelPoint(400, 60);
        source.Raise(WindowMoveSizeEventKind.Ended, 42);
        await WaitFor(() => result is not null);

        Assert.Equal(WindowProjectionRuntimeStatus.Failed, result!.Status);
        Assert.Equal("denied", result.ErrorMessage);
        Assert.Equal(0, controller.MoveCount);
    }

    [Fact]
    public async Task SingleDisplayDoesNotShowOverlay()
    {
        var source = new FakeMoveSizeSource();
        var overlay = new FakeOverlay();
        using var runtime = Create(
            source,
            overlay,
            new FakeWindowController(),
            Settings(),
            displayCatalog: new FakeDisplayCatalog(singleDisplay: true));

        source.Raise(WindowMoveSizeEventKind.Started, 42);
        await Task.Delay(40);

        Assert.Equal(0, overlay.ShowCount);
    }

    [Fact]
    public void Dispose_ReleasesEventSourceAndOverlay()
    {
        var source = new FakeMoveSizeSource();
        var overlay = new FakeOverlay();
        var runtime = Create(source, overlay, new FakeWindowController(), Settings());

        runtime.Dispose();

        Assert.True(source.IsDisposed);
        Assert.True(overlay.IsDisposed);
    }

    private static WindowProjectionSettings Settings(
        WindowProjectionTriggerMode triggerMode = WindowProjectionTriggerMode.Immediate,
        string? defaultTargetDisplayId = null,
        ProjectionLayout defaultLayout = ProjectionLayout.Maximized) =>
        new(true, triggerMode, true, new RelativePosition(.5, .1), defaultTargetDisplayId, defaultLayout);

    private WindowProjectionRuntime Create(
        FakeMoveSizeSource source,
        FakeOverlay overlay,
        FakeWindowController controller,
        WindowProjectionSettings settings,
        FakePointerState? pointer = null,
        IDisplayCatalog? displayCatalog = null,
        DisplayProfileService? displayProfileService = null)
    {
        pointer ??= new FakePointerState(new PixelPoint(50, 50));
        var store = new MemoryStore(settings);
        return new WindowProjectionRuntime(
            source,
            pointer,
            overlay,
            controller,
            displayCatalog ?? new FakeDisplayCatalog(),
            new WindowProjectionSettingsService(store),
            TimeSpan.FromMilliseconds(20),
            displayProfileService);
    }

    private static async Task WaitFor(Func<bool> condition)
    {
        var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (!condition() && DateTime.UtcNow < timeout) await Task.Delay(10);
        Assert.True(condition());
    }

    private sealed class FakeMoveSizeSource : IWindowMoveSizeEventSource
    {
        public bool IsDisposed { get; private set; }
        public event EventHandler<WindowMoveSizeEvent>? WindowChanged;
        public void Raise(WindowMoveSizeEventKind kind, nint handle) => WindowChanged?.Invoke(this, new WindowMoveSizeEvent(kind, handle));
        public void Dispose() => IsDisposed = true;
    }

    private sealed class FakePointerState(PixelPoint position) : IPointerState
    {
        public PixelPoint Position { get; set; } = position;
        public PixelPoint ReadPosition() => Position;
    }

    private sealed class FakeOverlay : IWindowProjectionOverlay
    {
        public int ShowCount => PortalShowCount + CommandShowCount;
        public int PortalShowCount { get; private set; }
        public int CommandShowCount { get; private set; }
        public int CommandUpdateCount { get; private set; }
        public int HideCount { get; private set; }
        public bool IsDisposed { get; private set; }
        public PixelRect PortalBounds { get; private set; }
        public WindowProjectionCommandLayout? Command { get; private set; }
        public ProjectionLayout? DefaultLayout { get; private set; }
        public string? SelectedTargetId => Command?.DisplayZones.FirstOrDefault(zone => zone.IsSelected).TargetDisplay?.StableId;
        public void ShowPortal(PixelRect bounds) { PortalShowCount++; PortalBounds = bounds; }
        public void ShowCommands(WindowProjectionCommandLayout command, ProjectionLayout defaultLayout, WindowProjectionHit hit) { CommandShowCount++; Command = command; DefaultLayout = defaultLayout; }
        public void UpdateCommands(WindowProjectionCommandLayout command, ProjectionLayout defaultLayout, WindowProjectionHit hit) { CommandUpdateCount++; Command = command; DefaultLayout = defaultLayout; }
        public void Hide() => HideCount++;
        public void Dispose() => IsDisposed = true;
    }

    private sealed class FakeWindowController : IApplicationWindowController
    {
        public Exception? MoveError { get; init; }
        public WindowPlacementSnapshot InitialPlacement { get; init; } = new(
            1,
            new PixelRect(10, 10, 70, 70),
            new PixelRect(10, 10, 70, 70));
        public WindowPlacementSnapshot? ReleasePlacement { get; init; }
        public WindowCapabilities Capabilities { get; init; } = WindowCapabilities.Standard;
        public int MoveCount { get; private set; }
        public int ReadCount { get; private set; }
        public nint LastHandle { get; private set; }
        public ApplicationProjectionPlan? Plan { get; private set; }
        public ApplicationWindowSnapshot? Read(nint windowHandle)
        {
            var placement = ReadCount++ == 0
                ? InitialPlacement
                : ReleasePlacement ?? InitialPlacement;
            return new ApplicationWindowSnapshot(
                windowHandle,
                new ProjectionApplicationIdentity(ApplicationIdentityKind.ExecutablePath, @"C:\Apps\app.exe"),
                "App",
                placement,
                Capabilities);
        }
        public IReadOnlyList<ApplicationWindowSnapshot> ReadAll() => [];
        public void Move(nint windowHandle, ApplicationProjectionPlan plan)
        {
            if (MoveError is not null) throw MoveError;
            MoveCount++;
            LastHandle = windowHandle;
            Plan = plan;
        }
    }

    private sealed class FakeDisplayCatalog(bool singleDisplay = false) : IDisplayCatalog
    {
        public IReadOnlyList<DisplaySnapshot> ReadAll() => singleDisplay
            ? [new("DISPLAY1", "Display 1", new PixelRect(0, 0, 1000, 800), new PixelRect(0, 0, 1000, 800), true, "stable-a")]
            : [new("DISPLAY1", "Display 1", new PixelRect(0, 0, 1000, 800), new PixelRect(0, 0, 1000, 800), true, "stable-a"), new("DISPLAY2", "Display 2", new PixelRect(1000, 0, 2000, 800), new PixelRect(1000, 0, 2000, 800), false, "stable-b")];
    }

    private sealed class MemoryStore(WindowProjectionSettings settings) : IWindowProjectionStore
    {
        public WindowProjectionSettings Load() => settings;
        public void Save(WindowProjectionSettings value) => settings = value;
    }

    private sealed class MemoryDisplayProfileStore : IDisplayProfileStore
    {
        public string FilePath => "memory://display-profiles";
        public IReadOnlyList<DisplayProfile> Profiles { get; private set; } = [];
        public IReadOnlyList<DisplayProfile> Load() => Profiles;
        public void Save(IReadOnlyList<DisplayProfile> profiles) => Profiles = profiles.ToArray();
    }
}
