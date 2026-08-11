using System.ComponentModel;
using MoniHop.Core.Displays;
using MoniHop.Core.Windows;
using MoniHop.Windows.Windows;

namespace MoniHop.Windows.Tests.Windows;

public sealed class WindowSwitchServiceTests
{
    [Fact]
    public void Switch_MovesForegroundWindowToNextDisplay()
    {
        var controller = new RecordingWindowController(
            42,
            Placement(new PixelRect(25, 25, 75, 75), showCommand: 1));
        var service = Service(controller);

        var result = service.Switch(DisplayDirection.Next, excludedWindowHandle: 99);

        Assert.Equal(WindowSwitchResult.Moved, result);
        Assert.Equal(42, controller.WrittenWindowHandle);
        Assert.Equal(new PixelRect(125, 25, 175, 75), controller.WrittenPlacement?.WindowRect);
        Assert.Equal((uint)1, controller.WrittenPlacement?.ShowCommand);
    }

    [Fact]
    public void Switch_MovesForegroundWindowToPreviousDisplayAndPreservesMaximizedState()
    {
        var controller = new RecordingWindowController(
            42,
            new WindowPlacementSnapshot(
                3,
                new PixelRect(100, 0, 200, 100),
                new PixelRect(125, 25, 175, 75)));
        var service = Service(controller);

        var result = service.Switch(DisplayDirection.Previous, excludedWindowHandle: 99);

        Assert.Equal(WindowSwitchResult.Moved, result);
        Assert.Equal(new PixelRect(0, 0, 100, 100), controller.WrittenPlacement?.WindowRect);
        Assert.Equal(new PixelRect(25, 25, 75, 75), controller.WrittenPlacement?.NormalRect);
        Assert.Equal((uint)3, controller.WrittenPlacement?.ShowCommand);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(42, true)]
    public void Switch_DoesNotWriteWhenForegroundWindowIsMissingOrExcluded(
        long foregroundWindow,
        bool excluded)
    {
        var controller = new RecordingWindowController(
            (nint)foregroundWindow,
            Placement(new PixelRect(25, 25, 75, 75), showCommand: 1));
        var service = Service(controller);

        var result = service.Switch(
            DisplayDirection.Next,
            excluded ? (nint)foregroundWindow : (nint)99);

        Assert.Equal(WindowSwitchResult.NoWindow, result);
        Assert.Null(controller.WrittenPlacement);
    }

    [Fact]
    public void Switch_DoesNotWriteWhenWindowIsIneligible()
    {
        var controller = new RecordingWindowController(42, placement: null);
        var service = Service(controller);

        var result = service.Switch(DisplayDirection.Next, excludedWindowHandle: 99);

        Assert.Equal(WindowSwitchResult.NoWindow, result);
        Assert.Null(controller.WrittenPlacement);
    }

    [Fact]
    public void Switch_DoesNotWriteWhenNoTargetDisplayExists()
    {
        var controller = new RecordingWindowController(
            42,
            Placement(new PixelRect(25, 25, 75, 75), showCommand: 1));
        var service = new WindowSwitchService(
            new StubDisplayCatalog([Display("DISPLAY1", new PixelRect(0, 0, 100, 100))]),
            controller);

        var result = service.Switch(DisplayDirection.Next, excludedWindowHandle: 99);

        Assert.Equal(WindowSwitchResult.NoTarget, result);
        Assert.Null(controller.WrittenPlacement);
    }

    [Fact]
    public void Switch_PropagatesWriteFailureWithoutRetrying()
    {
        var controller = new RecordingWindowController(
            42,
            Placement(new PixelRect(25, 25, 75, 75), showCommand: 1))
        {
            WriteException = new Win32Exception(5),
        };
        var service = Service(controller);

        var exception = Assert.Throws<Win32Exception>(
            () => service.Switch(DisplayDirection.Next, excludedWindowHandle: 99));

        Assert.Equal(5, exception.NativeErrorCode);
        Assert.Equal(1, controller.WriteCount);
    }

    private static WindowSwitchService Service(RecordingWindowController controller) =>
        new(new StubDisplayCatalog(TwoDisplays()), controller);

    private static WindowPlacementSnapshot Placement(PixelRect rect, uint showCommand) =>
        new(showCommand, rect, rect);

    private static IReadOnlyList<DisplaySnapshot> TwoDisplays() =>
    [
        Display("DISPLAY1", new PixelRect(0, 0, 100, 100)),
        Display("DISPLAY2", new PixelRect(100, 0, 200, 100)),
    ];

    private static DisplaySnapshot Display(string name, PixelRect bounds) =>
        new(name, name, bounds, bounds, name == "DISPLAY1");

    private sealed class StubDisplayCatalog(IReadOnlyList<DisplaySnapshot> displays) : IDisplayCatalog
    {
        public IReadOnlyList<DisplaySnapshot> ReadAll() => displays;
    }

    private sealed class RecordingWindowController(
        nint foregroundWindow,
        WindowPlacementSnapshot? placement) : IWindowController
    {
        public Exception? WriteException { get; init; }

        public int WriteCount { get; private set; }

        public nint? WrittenWindowHandle { get; private set; }

        public WindowPlacementSnapshot? WrittenPlacement { get; private set; }

        public nint GetForegroundWindow() => foregroundWindow;

        public WindowPlacementSnapshot? ReadPlacement(nint windowHandle) => placement;

        public void MoveWindow(nint windowHandle, WindowPlacementSnapshot placementToWrite)
        {
            WriteCount++;
            WrittenWindowHandle = windowHandle;
            WrittenPlacement = placementToWrite;

            if (WriteException is not null)
            {
                throw WriteException;
            }
        }
    }
}
