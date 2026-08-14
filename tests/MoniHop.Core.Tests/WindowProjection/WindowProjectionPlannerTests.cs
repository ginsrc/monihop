using MoniHop.Core.ApplicationProjection;
using MoniHop.Core.Displays;
using MoniHop.Core.WindowProjection;

namespace MoniHop.Core.Tests.WindowProjection;

public sealed class WindowProjectionPlannerTests
{
    [Fact]
    public void DefaultSettings_AreDisabledLockedAndUseEdgeDwell()
    {
        var settings = WindowProjectionSettings.Default;

        Assert.False(settings.IsEnabled);
        Assert.True(settings.IsPositionLocked);
        Assert.Equal(WindowProjectionTriggerMode.EdgeDwell, settings.TriggerMode);
        Assert.Null(settings.DefaultTargetDisplayId);
        Assert.Equal(ProjectionLayout.Maximized, settings.DefaultLayout);
        Assert.Equal(new RelativePosition(.5, 0), settings.RelativePosition);
        Assert.InRange(settings.RelativePosition.X, 0d, 1d);
        Assert.InRange(settings.RelativePosition.Y, 0d, 1d);
    }

    [Fact]
    public void Settings_NormalizeTargetAcceptKeepSizeAndRejectUnknownDefaultLayout()
    {
        var settings = new WindowProjectionSettings(
            true,
            WindowProjectionTriggerMode.Immediate,
            true,
            new RelativePosition(.5, .1),
            "  stable-b  ",
            ProjectionLayout.LeftHalf);

        Assert.Equal("stable-b", settings.DefaultTargetDisplayId);
        Assert.Equal(ProjectionLayout.LeftHalf, settings.DefaultLayout);
        var keepSize = new WindowProjectionSettings(
            true,
            WindowProjectionTriggerMode.Immediate,
            true,
            new RelativePosition(.5, .1),
            null,
            ProjectionLayout.KeepSize);
        Assert.Equal(ProjectionLayout.KeepSize, keepSize.DefaultLayout);
        Assert.Throws<ArgumentOutOfRangeException>(() => new WindowProjectionSettings(
            true,
            WindowProjectionTriggerMode.Immediate,
            true,
            new RelativePosition(.5, .1),
            null,
            (ProjectionLayout)99));
    }

    [Fact]
    public void CalculateOverlayBounds_UsesRelativeCenterAndClampsToWorkingArea()
    {
        var planner = new WindowProjectionPlanner();
        var workArea = new PixelRect(1920, 40, 3920, 1080);

        var centered = planner.CalculateOverlayBounds(
            workArea,
            new PixelSize(600, 180),
            new RelativePosition(0.75, 0.1));
        var clamped = planner.CalculateOverlayBounds(
            workArea,
            new PixelSize(600, 180),
            new RelativePosition(1, 1));

        Assert.Equal(new PixelRect(3120, 54, 3720, 234), centered);
        Assert.Equal(new PixelRect(3320, 900, 3920, 1080), clamped);
    }

    [Fact]
    public void CalculatePortalBounds_AnchorsToTopAndUsesHorizontalPositionOnly()
    {
        var planner = new WindowProjectionPlanner();
        var workingArea = new PixelRect(0, 0, 1000, 800);

        var bounds = planner.CalculatePortalBounds(
            workingArea,
            new PixelSize(64, 28),
            new RelativePosition(.5, .8));

        Assert.Equal(new PixelRect(468, 0, 532, 28), bounds);
    }

    [Fact]
    public void ToRelativePosition_RoundTripsOverlayCenter()
    {
        var planner = new WindowProjectionPlanner();
        var workArea = new PixelRect(-1600, 0, 0, 900);
        var bounds = new PixelRect(-1300, 90, -700, 270);

        var relative = planner.ToRelativePosition(workArea, bounds);
        var restored = planner.CalculateOverlayBounds(
            workArea,
            new PixelSize(bounds.Width, bounds.Height),
            relative);

        Assert.Equal(bounds, restored);
    }

    [Fact]
    public void CreateCommandLayout_SeparatesDefaultLayoutsAndDisplayTargets()
    {
        var displays = new[]
        {
            Display("stable-a", new PixelRect(0, 0, 1920, 1040)),
            Display("stable-b", new PixelRect(1920, 0, 3840, 1040)),
        };
        var planner = new WindowProjectionPlanner();
        var portal = new PixelRect(286, 24, 334, 32);
        var panel = new PixelRect(100, 24, 520, 144);

        var layout = planner.CreateCommandLayout(portal, panel, displays, displays[1]);

        Assert.Equal(portal, layout.PortalBounds);
        Assert.Equal(panel, layout.PanelBounds);
        Assert.Equal(new PixelRect(100, 24, 400, 100), layout.DefaultDropBounds);
        Assert.Collection(
            layout.LayoutZones,
            zone => Assert.Equal((ProjectionLayout.KeepSize, new PixelRect(100, 100, 175, 144)), (zone.Layout, zone.Bounds)),
            zone => Assert.Equal((ProjectionLayout.Maximized, new PixelRect(175, 100, 250, 144)), (zone.Layout, zone.Bounds)),
            zone => Assert.Equal((ProjectionLayout.LeftHalf, new PixelRect(250, 100, 325, 144)), (zone.Layout, zone.Bounds)),
            zone => Assert.Equal((ProjectionLayout.RightHalf, new PixelRect(325, 100, 400, 144)), (zone.Layout, zone.Bounds)));
        Assert.Collection(
            layout.DisplayZones,
            zone => Assert.Equal(("stable-a", new PixelRect(400, 24, 520, 84)), (zone.TargetDisplay.StableId, zone.Bounds)),
            zone =>
            {
                Assert.Equal(("stable-b", new PixelRect(400, 84, 520, 144)), (zone.TargetDisplay.StableId, zone.Bounds));
                Assert.True(zone.IsSelected);
            });
    }

    [Theory]
    [InlineData(150, 110, WindowProjectionHitKind.Layout, ProjectionLayout.KeepSize, null)]
    [InlineData(200, 110, WindowProjectionHitKind.Layout, ProjectionLayout.Maximized, null)]
    [InlineData(275, 110, WindowProjectionHitKind.Layout, ProjectionLayout.LeftHalf, null)]
    [InlineData(350, 110, WindowProjectionHitKind.Layout, ProjectionLayout.RightHalf, null)]
    [InlineData(450, 110, WindowProjectionHitKind.Display, null, "stable-b")]
    [InlineData(250, 60, WindowProjectionHitKind.DefaultDrop, null, null)]
    [InlineData(90, 60, WindowProjectionHitKind.None, null, null)]
    public void HitTestCommand_UsesCommandRegionPriority(
        int x,
        int y,
        WindowProjectionHitKind expectedKind,
        ProjectionLayout? expectedLayout,
        string? expectedDisplayId)
    {
        var displays = new[]
        {
            Display("stable-a", new PixelRect(0, 0, 1920, 1040)),
            Display("stable-b", new PixelRect(1920, 0, 3840, 1040)),
        };
        var planner = new WindowProjectionPlanner();
        var command = planner.CreateCommandLayout(
            new PixelRect(286, 24, 334, 32),
            new PixelRect(100, 24, 520, 144),
            displays,
            displays[0]);

        var hit = planner.HitTest(command, new PixelPoint(x, y));

        Assert.Equal(expectedKind, hit.Kind);
        Assert.Equal(expectedLayout, hit.Layout);
        Assert.Equal(expectedDisplayId, hit.TargetDisplay?.StableId);
    }

    [Fact]
    public void CalculateExpandedBounds_AnchorsToPortalAndStaysInsideWorkingArea()
    {
        var planner = new WindowProjectionPlanner();
        var workingArea = new PixelRect(0, 0, 1000, 800);

        var normal = planner.CalculateExpandedBounds(
            workingArea,
            new PixelSize(420, 120),
            new PixelRect(476, 40, 524, 48));
        var clamped = planner.CalculateExpandedBounds(
            workingArea,
            new PixelSize(420, 120),
            new PixelRect(952, 750, 1000, 758));

        Assert.Equal(new PixelRect(290, 48, 710, 168), normal);
        Assert.Equal(new PixelRect(580, 680, 1000, 800), clamped);
    }

    [Fact]
    public void CalculateExpandedOverlayBounds_ContainsPortalAndPanel()
    {
        var bounds = new WindowProjectionPlanner().CalculateExpandedOverlayBounds(
            new PixelRect(468, 0, 532, 28),
            new PixelRect(290, 28, 710, 148));

        Assert.Equal(new PixelRect(290, 0, 710, 148), bounds);
    }

    [Theory]
    [InlineData(ProjectionLayout.Maximized, 1920, 0, 3840, 1040)]
    [InlineData(ProjectionLayout.LeftHalf, 1920, 0, 2880, 1040)]
    [InlineData(ProjectionLayout.RightHalf, 2880, 0, 3840, 1040)]
    public void Plan_CalculatesSelectedDisplayLayout(
        ProjectionLayout layout,
        int left,
        int top,
        int right,
        int bottom)
    {
        var displays = new[]
        {
            Display("stable-a", new PixelRect(0, 0, 1920, 1040)),
            Display("stable-b", new PixelRect(1920, 0, 3840, 1040)),
        };
        var target = displays[1];

        var plan = new WindowProjectionPlanner().Plan(
            displays,
            target,
            new PixelRect(100, 100, 900, 700),
            layout);

        Assert.Equal(new PixelRect(left, top, right, bottom), plan.TargetRect);
        Assert.Equal(layout == ProjectionLayout.Maximized, plan.ShouldMaximize);
    }

    [Fact]
    public void Plan_KeepSizePreservesDragStartSizeAndRelativePosition()
    {
        var displays = new[]
        {
            Display("stable-a", new PixelRect(0, 0, 1920, 1040)),
            Display("stable-b", new PixelRect(1920, 0, 3840, 1040)),
        };

        var plan = new WindowProjectionPlanner().Plan(
            displays,
            displays[1],
            new PixelRect(100, 100, 900, 700),
            ProjectionLayout.KeepSize);

        Assert.Equal(new PixelRect(2020, 100, 2820, 700), plan.TargetRect);
        Assert.False(plan.ShouldMaximize);
    }

    private static DisplaySnapshot Display(string stableId, PixelRect workingArea) =>
        new(stableId, stableId, workingArea, workingArea, stableId == "stable-a", stableId);
}
