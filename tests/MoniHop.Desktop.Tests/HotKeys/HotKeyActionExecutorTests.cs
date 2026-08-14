using MoniHop.Core.ApplicationProjection;
using MoniHop.Core.Displays;
using MoniHop.Core.WindowProjection;
using MoniHop.Desktop.HotKeys;
using MoniHop.Desktop.Models;
using MoniHop.Desktop.Settings;
using MoniHop.Desktop.WindowProjection;
using MoniHop.Windows.ApplicationProjection;
using MoniHop.Windows.Cursors;
using MoniHop.Windows.Windows;
using ProjectionApplicationIdentity = MoniHop.Core.ApplicationProjection.ApplicationIdentity;

namespace MoniHop.Desktop.Tests.HotKeys;

public sealed class HotKeyActionExecutorTests
{
    [Theory]
    [InlineData(HotKeyCommand.CursorNext, 150)]
    [InlineData(HotKeyCommand.CursorPrevious, 150)]
    [InlineData(HotKeyCommand.CursorCenterActiveDisplay, 50)]
    public void Execute_MouseCommandsMoveCursor(HotKeyCommand command, int expectedX)
    {
        var context = new Context(cursorPosition: new PixelPoint(50, 25));

        var result = context.Executor.Execute(HotKeyCatalog.Get(command), 99);

        Assert.True(result.Succeeded);
        Assert.Equal(expectedX, context.Cursor.LastSetPosition?.X);
    }

    [Theory]
    [InlineData(HotKeyCommand.WindowAndCursorNext, 150)]
    [InlineData(HotKeyCommand.WindowAndCursorPrevious, 50)]
    public void Execute_WindowAndCursorMovesCursorOnlyAfterWindowMove(
        HotKeyCommand command,
        int expectedCursorX)
    {
        var initialRect = command == HotKeyCommand.WindowAndCursorNext
            ? new PixelRect(20, 20, 80, 80)
            : new PixelRect(120, 20, 180, 80);
        var context = new Context(windowPlacement: new WindowPlacementSnapshot(1, initialRect, initialRect));

        var result = context.Executor.Execute(HotKeyCatalog.Get(command), 99);

        Assert.True(result.Succeeded);
        Assert.Equal(expectedCursorX, context.Cursor.LastSetPosition?.X);
    }

    [Theory]
    [InlineData(HotKeyCommand.ProjectWindowKeepSize, ProjectionLayout.KeepSize)]
    [InlineData(HotKeyCommand.ProjectWindowMaximized, ProjectionLayout.Maximized)]
    [InlineData(HotKeyCommand.ProjectWindowLeftHalf, ProjectionLayout.LeftHalf)]
    [InlineData(HotKeyCommand.ProjectWindowRightHalf, ProjectionLayout.RightHalf)]
    public void Execute_ProjectionLayoutCommandsUseRequestedLayout(
        HotKeyCommand command,
        ProjectionLayout expectedLayout)
    {
        var context = new Context();

        var result = context.Executor.Execute(HotKeyCatalog.Get(command), 99);

        Assert.True(result.Succeeded);
        Assert.Equal(expectedLayout, context.ApplicationWindows.LastPlan?.Layout);
    }

    [Fact]
    public void Execute_DisplaySpecificCommandUsesStableTargetId()
    {
        var context = new Context();
        var definition = HotKeyCatalog.CreateForDisplays(
            [new HotKeyDisplayTarget("stable-a", "主屏", true)])
            .Single(item => item.Command == HotKeyCommand.ProjectWindowToSpecificDisplay);

        var result = context.Executor.Execute(definition, 99);

        Assert.True(result.Succeeded);
        Assert.Equal("stable-a", context.ApplicationWindows.LastPlan?.TargetDisplay.StableId);
    }

    [Fact]
    public void Execute_OpenProjectionPanelCapturesWindowBeforeRaisingRequest()
    {
        var context = new Context();
        WindowProjectionCandidate? candidate = null;
        context.Executor.ProjectionPanelRequested += (_, value) => candidate = value;

        var result = context.Executor.Execute(HotKeyCatalog.Get(HotKeyCommand.OpenProjectionPanel), 99);

        Assert.True(result.Succeeded);
        Assert.Equal((nint)42, candidate?.Snapshot.WindowHandle);
    }

    [Fact]
    public void Execute_RecallAndApplicationToggleAndSettingsAreWired()
    {
        var context = new Context(includeOffscreenWindow: true);
        var settingsRequested = false;
        context.Executor.SettingsRequested += (_, _) => settingsRequested = true;

        var recall = context.Executor.Execute(HotKeyCatalog.Get(HotKeyCommand.RecallOffscreenWindows), 99);
        var toggle = context.Executor.Execute(HotKeyCatalog.Get(HotKeyCommand.ToggleApplicationProjection), 99);
        var settings = context.Executor.Execute(HotKeyCatalog.Get(HotKeyCommand.OpenSettings), 99);

        Assert.True(recall.Succeeded);
        Assert.Contains("1", recall.StatusMessage, StringComparison.Ordinal);
        Assert.True(toggle.Succeeded);
        Assert.True(context.ApplicationSettings.Current.IsEnabled);
        Assert.True(settings.Succeeded);
        Assert.True(settingsRequested);
    }

    private sealed class Context
    {
        public Context(
            PixelPoint? cursorPosition = null,
            WindowPlacementSnapshot? windowPlacement = null,
            bool includeOffscreenWindow = false)
        {
            Displays = new FakeDisplayCatalog();
            Cursor = new FakeCursorController(cursorPosition ?? new PixelPoint(50, 50));
            NativeWindows = new FakeWindowController(windowPlacement ?? Placement(new PixelRect(20, 20, 80, 80)));
            ApplicationWindows = new FakeApplicationWindowController(
                NativeWindows,
                includeOffscreenWindow
                    ? [Snapshot(77, new PixelRect(400, 20, 500, 120))]
                    : []);
            ApplicationSettings = new ApplicationProjectionSettingsService(
                new MemoryApplicationStore(ApplicationProjectionSettings.Default));
            var projectionSettings = new WindowProjectionSettingsService(
                new MemoryWindowStore(new WindowProjectionSettings(
                    true,
                    WindowProjectionTriggerMode.Immediate,
                    true,
                    new RelativePosition(.5, 0),
                    "stable-b",
                    ProjectionLayout.Maximized)));
            Executor = new HotKeyActionExecutor(
                new CursorSwitchService(Displays, Cursor),
                new WindowSwitchService(Displays, NativeWindows),
                new WindowProjectionShortcutService(Displays, NativeWindows, ApplicationWindows, projectionSettings),
                new OffscreenWindowRecallService(Displays, ApplicationWindows, new CurrentDesktopFilter()),
                ApplicationSettings);
        }

        public FakeDisplayCatalog Displays { get; }
        public FakeCursorController Cursor { get; }
        public FakeWindowController NativeWindows { get; }
        public FakeApplicationWindowController ApplicationWindows { get; }
        public ApplicationProjectionSettingsService ApplicationSettings { get; }
        public HotKeyActionExecutor Executor { get; }
    }

    private static WindowPlacementSnapshot Placement(PixelRect rect) => new(1, rect, rect);

    private static ApplicationWindowSnapshot Snapshot(long handle, PixelRect rect) => new(
        new nint(handle),
        new ProjectionApplicationIdentity(ApplicationIdentityKind.ExecutablePath, $@"C:\Apps\{handle}.exe"),
        $"App {handle}",
        Placement(rect));

    private sealed class FakeDisplayCatalog : IDisplayCatalog
    {
        public IReadOnlyList<DisplaySnapshot> ReadAll() =>
        [
            new("DISPLAY1", "Display 1", new PixelRect(0, 0, 100, 100), new PixelRect(0, 0, 100, 100), true, "stable-a"),
            new("DISPLAY2", "Display 2", new PixelRect(100, 0, 200, 100), new PixelRect(100, 0, 200, 100), false, "stable-b"),
        ];
    }

    private sealed class FakeCursorController(PixelPoint position) : ICursorController
    {
        public PixelPoint? LastSetPosition { get; private set; }
        public PixelPoint GetPosition() => position;
        public void SetPosition(PixelPoint value) => LastSetPosition = value;
    }

    private sealed class FakeWindowController(WindowPlacementSnapshot placement) : IWindowController
    {
        public nint GetForegroundWindow() => 42;
        public WindowPlacementSnapshot? ReadPlacement(nint windowHandle) => placement;
        public void MoveWindow(nint windowHandle, WindowPlacementSnapshot target) => placement = target;
    }

    private sealed class FakeApplicationWindowController(
        IWindowController nativeWindows,
        IReadOnlyList<ApplicationWindowSnapshot> windows) : IApplicationWindowController
    {
        public ApplicationProjectionPlan? LastPlan { get; private set; }
        public ApplicationWindowSnapshot? Read(nint windowHandle)
        {
            var placement = nativeWindows.ReadPlacement(windowHandle);
            return placement is null ? null : new ApplicationWindowSnapshot(
                windowHandle,
                new ProjectionApplicationIdentity(ApplicationIdentityKind.ExecutablePath, @"C:\Apps\app.exe"),
                "App",
                placement.Value);
        }
        public IReadOnlyList<ApplicationWindowSnapshot> ReadAll() => windows;
        public IReadOnlyList<ApplicationWindowSnapshot> ReadAllWindows() => windows;
        public void Move(nint windowHandle, ApplicationProjectionPlan plan) => LastPlan = plan;
    }

    private sealed class CurrentDesktopFilter : IVirtualDesktopWindowFilter
    {
        public bool IsOnCurrentVirtualDesktop(nint windowHandle) => true;
    }

    private sealed class MemoryApplicationStore(ApplicationProjectionSettings settings) : IApplicationProjectionStore
    {
        public ApplicationProjectionSettings Load() => settings;
        public void Save(ApplicationProjectionSettings value) => settings = value;
    }

    private sealed class MemoryWindowStore(WindowProjectionSettings settings) : IWindowProjectionStore
    {
        public WindowProjectionSettings Load() => settings;
        public void Save(WindowProjectionSettings value) => settings = value;
    }
}
