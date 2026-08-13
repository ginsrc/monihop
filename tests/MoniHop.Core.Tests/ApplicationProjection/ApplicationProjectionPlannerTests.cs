using MoniHop.Core.ApplicationProjection;
using MoniHop.Core.Displays;

namespace MoniHop.Core.Tests.ApplicationProjection;

public sealed class ApplicationProjectionPlannerTests
{
    private static readonly ApplicationIdentity Browser =
        new(ApplicationIdentityKind.ExecutablePath, @"C:\Apps\browser.exe");

    [Fact]
    public void Plan_ReturnsNullWhenAutomaticProjectionIsDisabled()
    {
        var settings = new ApplicationProjectionSettings(false, null, []);

        var plan = new ApplicationProjectionPlanner().Plan(
            settings,
            Browser,
            Displays(),
            new PixelRect(20, 20, 80, 80));

        Assert.Null(plan);
    }

    [Fact]
    public void Plan_UsesEnabledApplicationRuleBeforeGlobalTarget()
    {
        var settings = new ApplicationProjectionSettings(
            true,
            "stable-a",
            [new ApplicationProjectionRule(Browser, "Browser", "stable-b", ProjectionLayout.RightHalf, true)]);

        var plan = new ApplicationProjectionPlanner().Plan(
            settings,
            new ApplicationIdentity(ApplicationIdentityKind.ExecutablePath, @"c:\apps\BROWSER.exe"),
            Displays(),
            new PixelRect(10, 10, 70, 70));

        Assert.NotNull(plan);
        Assert.Equal("stable-b", plan.TargetDisplay.StableId);
        Assert.Equal(new PixelRect(150, 0, 200, 100), plan.TargetRect);
        Assert.Equal(ProjectionRuleSource.Application, plan.RuleSource);
        Assert.False(plan.UsedPrimaryFallback);
    }

    [Fact]
    public void Plan_IgnoresPausedApplicationRuleAndUsesGlobalTarget()
    {
        var settings = new ApplicationProjectionSettings(
            true,
            "stable-a",
            [new ApplicationProjectionRule(Browser, "Browser", "stable-b", ProjectionLayout.Maximized, false)]);

        var plan = new ApplicationProjectionPlanner().Plan(
            settings,
            Browser,
            Displays(),
            new PixelRect(110, 10, 170, 70));

        Assert.NotNull(plan);
        Assert.Equal("stable-a", plan.TargetDisplay.StableId);
        Assert.Equal(ProjectionRuleSource.Global, plan.RuleSource);
        Assert.Equal(ProjectionLayout.KeepSize, plan.Layout);
    }

    [Fact]
    public void Plan_FallsBackToPrimaryWhenConfiguredTargetIsMissing()
    {
        var settings = new ApplicationProjectionSettings(true, "disconnected", []);

        var plan = new ApplicationProjectionPlanner().Plan(
            settings,
            Browser,
            Displays(),
            new PixelRect(110, 10, 170, 70));

        Assert.NotNull(plan);
        Assert.Equal("stable-a", plan.TargetDisplay.StableId);
        Assert.True(plan.UsedPrimaryFallback);
    }

    [Theory]
    [InlineData(ProjectionLayout.Maximized, 100, 0, 200, 100, true)]
    [InlineData(ProjectionLayout.LeftHalf, 100, 0, 150, 100, false)]
    [InlineData(ProjectionLayout.RightHalf, 150, 0, 200, 100, false)]
    [InlineData(ProjectionLayout.KeepSize, 110, 10, 170, 70, false)]
    public void Plan_CalculatesSupportedLayouts(
        ProjectionLayout layout,
        int left,
        int top,
        int right,
        int bottom,
        bool maximize)
    {
        var settings = new ApplicationProjectionSettings(
            true,
            null,
            [new ApplicationProjectionRule(Browser, "Browser", "stable-b", layout, true)]);

        var plan = new ApplicationProjectionPlanner().Plan(
            settings,
            Browser,
            Displays(),
            new PixelRect(10, 10, 70, 70));

        Assert.NotNull(plan);
        Assert.Equal(new PixelRect(left, top, right, bottom), plan.TargetRect);
        Assert.Equal(maximize, plan.ShouldMaximize);
    }

    [Fact]
    public void Plan_ReturnsNullInSingleDisplayMode()
    {
        var settings = new ApplicationProjectionSettings(true, null, []);

        var plan = new ApplicationProjectionPlanner().Plan(
            settings,
            Browser,
            [Displays()[0]],
            new PixelRect(10, 10, 70, 70));

        Assert.Null(plan);
    }

    private static IReadOnlyList<DisplaySnapshot> Displays() =>
    [
        new DisplaySnapshot(
            "DISPLAY1",
            "Display 1",
            new PixelRect(0, 0, 100, 100),
            new PixelRect(0, 0, 100, 100),
            true,
            "stable-a"),
        new DisplaySnapshot(
            "DISPLAY2",
            "Display 2",
            new PixelRect(100, 0, 200, 100),
            new PixelRect(100, 0, 200, 100),
            false,
            "stable-b"),
    ];
}
