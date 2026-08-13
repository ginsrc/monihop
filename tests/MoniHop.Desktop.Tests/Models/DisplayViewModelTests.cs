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
            CreateDisplay("\\\\.\\DISPLAY1", "Internal display", 1920, 1200, true),
            CreateDisplay("\\\\.\\DISPLAY2", "External display", 2560, 1440, false),
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
        bool isPrimary) =>
        new(
            deviceName,
            displayName,
            new PixelRect(0, 0, width, height),
            new PixelRect(0, 0, width, height - 40),
            isPrimary);
}
