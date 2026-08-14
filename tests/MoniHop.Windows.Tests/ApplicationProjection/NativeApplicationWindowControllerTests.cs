using System.Runtime.Versioning;
using MoniHop.Core.ApplicationProjection;
using MoniHop.Core.Displays;
using MoniHop.Windows.ApplicationProjection;
using MoniHop.Windows.Windows;

namespace MoniHop.Windows.Tests.ApplicationProjection;

[SupportedOSPlatform("windows")]
public sealed class NativeApplicationWindowControllerTests
{
    [Theory]
    [InlineData(true, false, 0, 0, 0x00C00000, 0, "Chrome_WidgetWin_1", true)]
    [InlineData(true, false, 0, 0, 0x170F0000, 0x00000100, "OpusApp", true)]
    [InlineData(true, false, 42, 0, 0x00C00000, 0, "Chrome_RenderWidgetHostHWND", false)]
    [InlineData(true, false, 0, 42, 0x00C00000, 0, "Chrome_RenderWidgetHostHWND", false)]
    [InlineData(true, false, 0, 0, 0x00C00000, 0x00000080, "toolwindow", false)]
    [InlineData(true, false, 0, 0, 0x00C00000, 0x08000000, "overlay", false)]
    [InlineData(true, false, 0, 0, 0x00C00000, 0, "#32768", false)]
    [InlineData(true, false, 0, 0, 0, 0, "screenshot-overlay", false)]
    [InlineData(false, false, 0, 0, 0x00C00000, 0, "Chrome_WidgetWin_1", false)]
    public void IsApplicationWindowCandidate_RejectsTransientOrNestedWindows(
        bool isVisible,
        bool isIconic,
        long owner,
        long parent,
        int windowStyle,
        int extendedStyle,
        string className,
        bool expected)
    {
        Assert.Equal(
            expected,
            NativeWindowController.IsApplicationWindowCandidate(
                isVisible,
                isIconic,
                new nint(owner),
                new nint(parent),
                windowStyle,
                extendedStyle,
                className));
    }

    [Fact]
    public void Read_ReturnsNullForMissingWindowHandle()
    {
        Assert.Null(new NativeApplicationWindowController().Read(0));
    }

    [Fact]
    public void Move_RejectsMissingWindowHandle()
    {
        var plan = new ApplicationProjectionPlan(
            new DisplaySnapshot(
                "DISPLAY1",
                "Display 1",
                new PixelRect(0, 0, 100, 100),
                new PixelRect(0, 0, 100, 100),
                true),
            new PixelRect(0, 0, 100, 100),
            ProjectionLayout.Maximized,
            true,
            ProjectionRuleSource.Global,
            false);

        var exception = Assert.Throws<ArgumentException>(
            () => new NativeApplicationWindowController().Move(0, plan));

        Assert.Equal("windowHandle", exception.ParamName);
    }

    [Fact]
    public void Move_KeepSizePreservesMaximizedWindowState()
    {
        var nativeWindow = new RecordingWindowController(new WindowPlacementSnapshot(
            3,
            new PixelRect(0, 0, 100, 100),
            new PixelRect(10, 10, 70, 70)));
        var controller = new NativeApplicationWindowController(nativeWindow);
        var plan = Plan(ProjectionLayout.KeepSize, shouldMaximize: false);

        controller.Move(42, plan);

        Assert.Equal((uint)3, nativeWindow.WrittenPlacement?.ShowCommand);
        Assert.Equal(new PixelRect(110, 10, 170, 70), nativeWindow.WrittenPlacement?.NormalRect);
    }

    private static ApplicationProjectionPlan Plan(ProjectionLayout layout, bool shouldMaximize) =>
        new(
            new DisplaySnapshot(
                "DISPLAY2",
                "Display 2",
                new PixelRect(100, 0, 200, 100),
                new PixelRect(100, 0, 200, 100),
                false),
            new PixelRect(110, 10, 170, 70),
            layout,
            shouldMaximize,
            ProjectionRuleSource.Global,
            false);

    private sealed class RecordingWindowController(WindowPlacementSnapshot placement) : IWindowController
    {
        public WindowPlacementSnapshot? WrittenPlacement { get; private set; }
        public nint GetForegroundWindow() => 42;
        public WindowPlacementSnapshot? ReadPlacement(nint windowHandle) => placement;
        public void MoveWindow(nint windowHandle, WindowPlacementSnapshot placementToWrite) =>
            WrittenPlacement = placementToWrite;
    }
}
