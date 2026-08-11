using MoniHop.Core.Displays;
using MoniHop.Core.Windows;

namespace MoniHop.Core.Tests.Windows;

public sealed class WindowSwitchPlannerTests
{
    [Fact]
    public void PlanNext_MapsWindowAcrossDifferentWorkingAreas()
    {
        var displays = new[]
        {
            Display("DISPLAY1", new PixelRect(0, 0, 1920, 1080), new PixelRect(0, 0, 1920, 1040)),
            Display("DISPLAY2", new PixelRect(1920, 0, 5120, 1800), new PixelRect(1920, 0, 5120, 1760)),
        };

        var result = new WindowSwitchPlanner().Plan(
            displays,
            new PixelRect(710, 370, 1210, 870),
            DisplayDirection.Next);

        Assert.NotNull(result);
        Assert.Equal("DISPLAY2", result.Value.TargetDisplay.DeviceName);
        Assert.Equal(new PixelRect(3103, 626, 3603, 1126), result.Value.TargetNormalRect);
    }

    [Fact]
    public void PlanPrevious_CyclesFromFirstToLastByDisplayOrder()
    {
        var displays = new[]
        {
            Display("B", new PixelRect(0, 0, 100, 100)),
            Display("A", new PixelRect(0, -100, 100, 0)),
            Display("C", new PixelRect(100, 0, 200, 100)),
        };

        var result = new WindowSwitchPlanner().Plan(
            displays,
            new PixelRect(25, -75, 75, -25),
            DisplayDirection.Previous);

        Assert.NotNull(result);
        Assert.Equal("C", result.Value.TargetDisplay.DeviceName);
        Assert.Equal(new PixelRect(125, 25, 175, 75), result.Value.TargetNormalRect);
    }

    [Fact]
    public void PlanNext_ClampsOversizedWindowToTargetWorkingAreaOrigin()
    {
        var displays = new[]
        {
            Display("DISPLAY1", new PixelRect(-200, 0, 0, 200)),
            Display("DISPLAY2", new PixelRect(0, 0, 100, 100)),
        };

        var result = new WindowSwitchPlanner().Plan(
            displays,
            new PixelRect(-190, 10, -40, 160),
            DisplayDirection.Next);

        Assert.NotNull(result);
        Assert.Equal(new PixelRect(0, 0, 150, 150), result.Value.TargetNormalRect);
    }

    [Fact]
    public void Plan_ReturnsNullForSingleDisplayOrUnknownWindowCenter()
    {
        var display = Display("DISPLAY1", new PixelRect(0, 0, 100, 100));
        var planner = new WindowSwitchPlanner();

        Assert.Null(planner.Plan(
            [display],
            new PixelRect(10, 10, 50, 50),
            DisplayDirection.Next));
        Assert.Null(planner.Plan(
            [display, Display("DISPLAY2", new PixelRect(100, 0, 200, 100))],
            new PixelRect(250, 10, 300, 60),
            DisplayDirection.Next));
    }

    private static DisplaySnapshot Display(
        string deviceName,
        PixelRect bounds,
        PixelRect? workingArea = null) =>
        new(deviceName, deviceName, bounds, workingArea ?? bounds, deviceName == "DISPLAY1");
}
