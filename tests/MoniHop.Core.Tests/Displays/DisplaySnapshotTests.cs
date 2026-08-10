using MoniHop.Core.Displays;

namespace MoniHop.Core.Tests.Displays;

public sealed class DisplaySnapshotTests
{
    [Fact]
    public void PixelRect_ExposesWidthAndHeight()
    {
        var bounds = new PixelRect(-1920, 0, 0, 1080);

        Assert.Equal(1920, bounds.Width);
        Assert.Equal(1080, bounds.Height);
    }

    [Fact]
    public void DisplaySnapshot_RejectsEmptyDeviceName()
    {
        var bounds = new PixelRect(0, 0, 1920, 1080);

        var exception = Assert.Throws<ArgumentException>(
            () => new DisplaySnapshot(" ", "Display 1", bounds, bounds, true));

        Assert.Equal("deviceName", exception.ParamName);
    }

    [Fact]
    public void DisplaySnapshot_RejectsWorkingAreaOutsideBounds()
    {
        var bounds = new PixelRect(0, 0, 1920, 1080);
        var invalidWorkingArea = new PixelRect(0, 0, 2560, 1080);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DisplaySnapshot(
                "DISPLAY1",
                "Display 1",
                bounds,
                invalidWorkingArea,
                true));
    }
}
