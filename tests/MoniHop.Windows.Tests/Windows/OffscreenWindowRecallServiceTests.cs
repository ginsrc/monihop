using MoniHop.Core.ApplicationProjection;
using MoniHop.Core.Displays;
using MoniHop.Windows.ApplicationProjection;
using MoniHop.Windows.Windows;

namespace MoniHop.Windows.Tests.Windows;

public sealed class OffscreenWindowRecallServiceTests
{
    [Fact]
    public void Recall_MovesOnlyFullyOffscreenWindowsOnCurrentVirtualDesktop()
    {
        var controller = new FakeApplicationWindowController(
        [
            Window(1, new PixelRect(20, 20, 120, 120)),
            Window(2, new PixelRect(2200, 20, 2400, 220)),
            Window(3, new PixelRect(2300, 20, 2500, 220)),
            Window(99, new PixelRect(2400, 20, 2600, 220)),
        ]);
        var virtualDesktops = new FakeVirtualDesktopFilter([1, 2, 99]);
        var service = new OffscreenWindowRecallService(
            new FakeDisplayCatalog(), controller, virtualDesktops);

        var result = service.Recall(excludedWindowHandle: 99);

        Assert.Equal(1, result.MovedCount);
        Assert.Equal(1, result.SkippedOtherDesktopCount);
        Assert.Equal((nint)2, Assert.Single(controller.MovedHandles));
        Assert.Equal("stable-a", controller.Plans[0].TargetDisplay.StableId);
        Assert.Equal(ProjectionLayout.KeepSize, controller.Plans[0].Layout);
    }

    [Fact]
    public void Recall_NoDisplaysDoesNotEnumerateOrMoveWindows()
    {
        var controller = new FakeApplicationWindowController([Window(1, new PixelRect(2000, 0, 2100, 100))]);
        var service = new OffscreenWindowRecallService(
            new EmptyDisplayCatalog(), controller, new FakeVirtualDesktopFilter([1]));

        var result = service.Recall(99);

        Assert.Equal(OffscreenWindowRecallStatus.NoDisplay, result.Status);
        Assert.Empty(controller.MovedHandles);
    }

    private static ApplicationWindowSnapshot Window(long handle, PixelRect rect) => new(
        new nint(handle),
        new ApplicationIdentity(ApplicationIdentityKind.ExecutablePath, $@"C:\Apps\{handle}.exe"),
        $"App {handle}",
        new WindowPlacementSnapshot(1, rect, rect));

    private sealed class FakeApplicationWindowController(IReadOnlyList<ApplicationWindowSnapshot> windows)
        : IApplicationWindowController
    {
        public List<nint> MovedHandles { get; } = [];
        public List<ApplicationProjectionPlan> Plans { get; } = [];
        public ApplicationWindowSnapshot? Read(nint windowHandle) => windows.FirstOrDefault(item => item.WindowHandle == windowHandle);
        public IReadOnlyList<ApplicationWindowSnapshot> ReadAll() => windows;
        public IReadOnlyList<ApplicationWindowSnapshot> ReadAllWindows() => windows;
        public void Move(nint windowHandle, ApplicationProjectionPlan plan)
        {
            MovedHandles.Add(windowHandle);
            Plans.Add(plan);
        }
    }

    private sealed class FakeVirtualDesktopFilter(IEnumerable<long> handles) : IVirtualDesktopWindowFilter
    {
        private readonly HashSet<nint> _handles = handles.Select(value => new nint(value)).ToHashSet();
        public bool IsOnCurrentVirtualDesktop(nint windowHandle) => _handles.Contains(windowHandle);
    }

    private sealed class FakeDisplayCatalog : IDisplayCatalog
    {
        public IReadOnlyList<DisplaySnapshot> ReadAll() =>
        [new("DISPLAY1", "Display 1", new PixelRect(0, 0, 1000, 800), new PixelRect(0, 0, 1000, 760), true, "stable-a")];
    }

    private sealed class EmptyDisplayCatalog : IDisplayCatalog
    {
        public IReadOnlyList<DisplaySnapshot> ReadAll() => [];
    }
}
