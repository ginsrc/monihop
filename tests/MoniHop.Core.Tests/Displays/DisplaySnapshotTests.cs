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

    [Fact]
    public void DisplaySnapshot_UsesStableIdWhenProvided()
    {
        var bounds = new PixelRect(0, 0, 1920, 1080);

        var snapshot = new DisplaySnapshot(
            "\\\\.\\DISPLAY1",
            "Display 1",
            bounds,
            bounds,
            true,
            "MONITOR#ABC#123");

        Assert.Equal("MONITOR#ABC#123", snapshot.StableId);
    }

    [Fact]
    public void DisplaySnapshot_StoresDisplayMetrics()
    {
        var bounds = new PixelRect(0, 0, 2560, 1440);

        var snapshot = new DisplaySnapshot(
            "\\\\.\\DISPLAY1",
            "Display 1",
            bounds,
            bounds,
            true,
            refreshRateHz: 144,
            scalePercent: 150,
            orientation: DisplayOrientation.Landscape,
            physicalWidthMillimeters: 600,
            physicalHeightMillimeters: 340,
            resolutionWidth: 2560,
            resolutionHeight: 1440);

        Assert.Equal(2560, snapshot.ResolutionWidth);
        Assert.Equal(1440, snapshot.ResolutionHeight);
        Assert.Equal(144, snapshot.RefreshRateHz);
        Assert.Equal(150, snapshot.ScalePercent);
        Assert.Equal(DisplayOrientation.Landscape, snapshot.Orientation);
        Assert.Equal(600, snapshot.PhysicalWidthMillimeters);
        Assert.Equal(340, snapshot.PhysicalHeightMillimeters);
    }

    [Fact]
    public void DisplaySnapshot_FallsBackToBoundsWhenNativeResolutionIsUnavailable()
    {
        var bounds = new PixelRect(0, 0, 1920, 1200);

        var snapshot = new DisplaySnapshot("DISPLAY1", "Display 1", bounds, bounds, true);

        Assert.Equal(1920, snapshot.ResolutionWidth);
        Assert.Equal(1200, snapshot.ResolutionHeight);
    }
}
