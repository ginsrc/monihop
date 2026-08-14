using MoniHop.Core.ApplicationProjection;
using MoniHop.Core.Displays;
using MoniHop.Core.WindowProjection;
using MoniHop.Desktop.Settings;
using MoniHop.Desktop.WindowProjection;
using MoniHop.Windows.ApplicationProjection;
using MoniHop.Windows.Windows;
using ProjectionApplicationIdentity = MoniHop.Core.ApplicationProjection.ApplicationIdentity;

namespace MoniHop.Desktop.Tests.WindowProjection;

public sealed class WindowProjectionShortcutServiceTests
{
    [Fact]
    public void ProjectCurrent_UsesConfiguredDefaultTargetAndLayout()
    {
        var windows = new FakeApplicationWindowController();
        var service = Create(windows, Settings("stable-b", ProjectionLayout.RightHalf));

        var result = service.ProjectCurrent(99);

        Assert.Equal(WindowProjectionShortcutStatus.Moved, result.Status);
        Assert.Equal("stable-b", windows.Plan?.TargetDisplay.StableId);
        Assert.Equal(ProjectionLayout.RightHalf, windows.Plan?.Layout);
    }

    [Fact]
    public void ProjectCurrent_UsesNextDisplayWhenDefaultTargetIsUnset()
    {
        var windows = new FakeApplicationWindowController();
        var service = Create(windows, Settings(null, ProjectionLayout.Maximized));

        var result = service.ProjectCurrent(99);

        Assert.Equal(WindowProjectionShortcutStatus.Moved, result.Status);
        Assert.Equal("stable-b", windows.Plan?.TargetDisplay.StableId);
    }

    [Fact]
    public void ProjectCurrent_LayoutOverrideFallsBackForFixedSizeWindow()
    {
        var windows = new FakeApplicationWindowController
        {
            Capabilities = new WindowCapabilities(false, false),
        };
        var service = Create(windows, Settings("stable-b", ProjectionLayout.RightHalf));

        var result = service.ProjectCurrent(99, ProjectionLayout.Maximized);

        Assert.Equal(WindowProjectionShortcutStatus.Moved, result.Status);
        Assert.Equal(ProjectionLayout.KeepSize, windows.Plan?.Layout);
        Assert.False(windows.Plan?.ShouldMaximize);
    }

    [Fact]
    public void ProjectCurrent_MissingExplicitTargetDoesNotMove()
    {
        var windows = new FakeApplicationWindowController();
        var service = Create(windows, Settings(null, ProjectionLayout.KeepSize));

        var result = service.ProjectCurrent(99, targetDisplayId: "missing");

        Assert.Equal(WindowProjectionShortcutStatus.TargetUnavailable, result.Status);
        Assert.Null(windows.Plan);
    }

    [Fact]
    public void CaptureCurrent_PreservesWindowForPanelAfterFocusChanges()
    {
        var foreground = new FakeForegroundWindowController { Foreground = 42 };
        var windows = new FakeApplicationWindowController();
        var service = Create(windows, Settings(null, ProjectionLayout.KeepSize), foreground);

        var candidate = service.CaptureCurrent(99);
        foreground.Foreground = 777;
        var result = service.Project(candidate!, ProjectionLayout.LeftHalf, "stable-b");

        Assert.Equal(WindowProjectionShortcutStatus.Moved, result.Status);
        Assert.Equal((nint)42, windows.MovedHandle);
        Assert.Equal(ProjectionLayout.LeftHalf, windows.Plan?.Layout);
    }

    [Fact]
    public void ResolveDefaultTargetId_ForPanelUsesNextDisplayWhenUnconfigured()
    {
        var windows = new FakeApplicationWindowController();
        var service = Create(windows, Settings(null, ProjectionLayout.KeepSize));
        var candidate = service.CaptureCurrent(99);

        var targetId = service.ResolveDefaultTargetId(candidate!);

        Assert.Equal("stable-b", targetId);
    }

    private static WindowProjectionShortcutService Create(
        FakeApplicationWindowController windows,
        WindowProjectionSettings settings,
        FakeForegroundWindowController? foreground = null) =>
        new(
            new FakeDisplayCatalog(),
            foreground ?? new FakeForegroundWindowController(),
            windows,
            new WindowProjectionSettingsService(new MemoryStore(settings)));

    private static WindowProjectionSettings Settings(string? target, ProjectionLayout layout) =>
        new(true, WindowProjectionTriggerMode.Immediate, true, new RelativePosition(.5, 0), target, layout);

    private sealed class FakeForegroundWindowController : IWindowController
    {
        public nint Foreground { get; set; } = 42;
        public nint GetForegroundWindow() => Foreground;
        public WindowPlacementSnapshot? ReadPlacement(nint windowHandle) => null;
        public void MoveWindow(nint windowHandle, WindowPlacementSnapshot placement) { }
    }

    private sealed class FakeApplicationWindowController : IApplicationWindowController
    {
        public WindowCapabilities Capabilities { get; init; } = WindowCapabilities.Standard;
        public nint MovedHandle { get; private set; }
        public ApplicationProjectionPlan? Plan { get; private set; }
        public ApplicationWindowSnapshot? Read(nint windowHandle) => new(
            windowHandle,
            new ProjectionApplicationIdentity(ApplicationIdentityKind.ExecutablePath, @"C:\Apps\app.exe"),
            "App",
            new WindowPlacementSnapshot(1, new PixelRect(100, 100, 500, 400), new PixelRect(100, 100, 500, 400)),
            Capabilities);
        public IReadOnlyList<ApplicationWindowSnapshot> ReadAll() => [];
        public void Move(nint windowHandle, ApplicationProjectionPlan plan)
        {
            MovedHandle = windowHandle;
            Plan = plan;
        }
    }

    private sealed class FakeDisplayCatalog : IDisplayCatalog
    {
        public IReadOnlyList<DisplaySnapshot> ReadAll() =>
        [
            new("DISPLAY1", "Display 1", new PixelRect(0, 0, 1000, 800), new PixelRect(0, 0, 1000, 760), true, "stable-a"),
            new("DISPLAY2", "Display 2", new PixelRect(1000, 0, 2000, 800), new PixelRect(1000, 0, 2000, 760), false, "stable-b"),
        ];
    }

    private sealed class MemoryStore(WindowProjectionSettings settings) : IWindowProjectionStore
    {
        public WindowProjectionSettings Load() => settings;
        public void Save(WindowProjectionSettings value) => settings = value;
    }
}
