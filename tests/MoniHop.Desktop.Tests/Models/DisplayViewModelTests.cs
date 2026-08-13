using MoniHop.Core.Displays;
using MoniHop.Desktop.Models;

namespace MoniHop.Desktop.Tests.Models;

public sealed class DisplayViewModelTests
{
    [Fact]
    public void CreateAll_MapsRealDisplaysInEnumerationOrder()
    {
        var displays = new[]
        {
            CreateDisplay("\\\\.\\DISPLAY1", "Internal display", 1920, 1200, true,
                60, 125, DisplayOrientation.Landscape, 310, 174),
            CreateDisplay("\\\\.\\DISPLAY2", "External display", 3200, 1800, false,
                scalePercent: 100, resolutionWidth: 2560, resolutionHeight: 1440),
        };

        var result = DisplayViewModel.CreateAll(displays);

        Assert.Collection(
            result,
            first =>
            {
                Assert.Equal("屏幕 1", first.ScreenLabel);
                Assert.Equal("Internal display", first.Name);
                Assert.Equal("1920 × 1200", first.Resolution);
                Assert.Equal("Windows 主显示器", first.PrimaryStatus);
                Assert.Equal("60 Hz", first.RefreshRate);
                Assert.Equal("125%", first.Scale);
                Assert.Equal("横向", first.Orientation);
                Assert.Equal("14.0 英寸", first.PhysicalSize);
                Assert.Equal("310 × 174 mm", first.PhysicalSizeDetail);
            },
            second =>
            {
                Assert.Equal("屏幕 2", second.ScreenLabel);
                Assert.Equal("External display", second.Name);
                Assert.Equal("2560 × 1440", second.Resolution);
                Assert.Equal("普通显示器", second.PrimaryStatus);
            });
    }

    private static DisplaySnapshot CreateDisplay(
        string deviceName,
        string displayName,
        int width,
        int height,
        bool isPrimary,
        int? refreshRateHz = null,
        int? scalePercent = null,
        DisplayOrientation orientation = DisplayOrientation.Unknown,
        int? physicalWidthMillimeters = null,
        int? physicalHeightMillimeters = null,
        int? resolutionWidth = null,
        int? resolutionHeight = null) =>
        new(
            deviceName,
            displayName,
            new PixelRect(0, 0, width, height),
            new PixelRect(0, 0, width, height - 40),
            isPrimary,
            refreshRateHz: refreshRateHz,
            scalePercent: scalePercent,
            orientation: orientation,
            physicalWidthMillimeters: physicalWidthMillimeters,
            physicalHeightMillimeters: physicalHeightMillimeters,
            resolutionWidth: resolutionWidth,
            resolutionHeight: resolutionHeight);
}
