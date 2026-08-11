using MoniHop.Core.Displays;
using MoniHop.Windows.Windows;

namespace MoniHop.Windows.Tests.Windows;

public sealed class NativeWindowControllerTests
{
    [Fact]
    public void ReadPlacement_ReturnsNullForMissingWindowHandle()
    {
        Assert.Null(new NativeWindowController().ReadPlacement(0));
    }

    [Fact]
    public void MoveWindow_RejectsMissingWindowHandle()
    {
        var placement = new WindowPlacementSnapshot(
            1,
            new PixelRect(0, 0, 100, 100),
            new PixelRect(0, 0, 100, 100));

        var exception = Assert.Throws<ArgumentException>(
            () => new NativeWindowController().MoveWindow(0, placement));

        Assert.Equal("windowHandle", exception.ParamName);
    }
}
