using MoniHop.Core.Cursors;
using MoniHop.Core.Displays;
using MoniHop.Core.Windows;

namespace MoniHop.Core.Tests.Cursors;

public sealed class CursorSwitchPlannerTests
{
    [Fact]
    public void PlanNext_MapsRelativePositionAcrossDifferentResolutions()
    {
        var displays = new[]
        {
            Display("DISPLAY1", new PixelRect(0, 0, 1920, 1080)),
            Display("DISPLAY2", new PixelRect(1920, 0, 5120, 1800)),
        };

        var result = new CursorSwitchPlanner().PlanNext(displays, new PixelPoint(960, 540));

        Assert.Equal(new PixelPoint(3520, 900), result);
    }

    [Fact]
    public void PlanNext_CyclesByLeftThenTopThenDeviceName()
    {
        var displays = new[]
        {
            Display("A", new PixelRect(0, 0, 100, 100)),
            Display("B", new PixelRect(0, -100, 100, 0)),
            Display("C", new PixelRect(100, 0, 200, 100)),
        };

        var planner = new CursorSwitchPlanner();

        Assert.Equal(new PixelPoint(150, 50), planner.PlanNext(displays, new PixelPoint(50, 50)));
        Assert.Equal(new PixelPoint(50, -50), planner.PlanNext(displays, new PixelPoint(150, 50)));
    }

    [Fact]
    public void PlanNext_ReturnsNullForSingleDisplayOrUnknownPosition()
    {
        var display = Display("DISPLAY1", new PixelRect(0, 0, 100, 100));
        var planner = new CursorSwitchPlanner();

        Assert.Null(planner.PlanNext([display], new PixelPoint(50, 50)));
        Assert.Null(planner.PlanNext(
            [display, Display("DISPLAY2", new PixelRect(100, 0, 200, 100))],
            new PixelPoint(300, 50)));
    }

    [Fact]
    public void PlanNext_ClampsMappedPositionToTargetLastPixel()
    {
        var displays = new[]
        {
            Display("DISPLAY1", new PixelRect(0, 0, 100, 100)),
            Display("DISPLAY2", new PixelRect(100, 0, 150, 50)),
        };

        var result = new CursorSwitchPlanner().PlanNext(displays, new PixelPoint(99, 99));

        Assert.Equal(new PixelPoint(149, 49), result);
    }

    [Fact]
    public void Plan_PreviousCyclesInReverseOrder()
    {
        var displays = new[]
        {
            Display("DISPLAY1", new PixelRect(0, 0, 100, 100)),
            Display("DISPLAY2", new PixelRect(100, 0, 200, 100)),
            Display("DISPLAY3", new PixelRect(200, 0, 300, 100)),
        };

        var result = new CursorSwitchPlanner().Plan(
            displays,
            new PixelPoint(150, 25),
            DisplayDirection.Previous);

        Assert.Equal(new PixelPoint(50, 25), result);
    }

    [Fact]
    public void PlanCenter_ReturnsCenterOfDisplayContainingCursor()
    {
        var displays = new[]
        {
            Display("DISPLAY1", new PixelRect(0, 0, 100, 100)),
            Display("DISPLAY2", new PixelRect(100, -50, 300, 150)),
        };

        var result = new CursorSwitchPlanner().PlanCenter(displays, new PixelPoint(120, 0));

        Assert.Equal(new PixelPoint(200, 50), result);
    }

    private static DisplaySnapshot Display(string deviceName, PixelRect bounds) =>
        new(deviceName, deviceName, bounds, bounds, deviceName == "DISPLAY1");
}
