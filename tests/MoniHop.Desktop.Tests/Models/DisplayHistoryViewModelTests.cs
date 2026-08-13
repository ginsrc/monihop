using MoniHop.Core.Displays;
using MoniHop.Core.ApplicationProjection;
using MoniHop.Desktop.Models;
using ProjectionApplicationIdentity = MoniHop.Core.ApplicationProjection.ApplicationIdentity;

namespace MoniHop.Desktop.Tests.Models;

public sealed class DisplayHistoryViewModelTests
{
    [Fact]
    public void Refresh_ReplacesConnectedCardsAndRetainsDisconnectedHistory()
    {
        var viewModel = new DisplayHistoryViewModel();
        var now = new DateTimeOffset(2026, 8, 13, 10, 0, 0, TimeSpan.Zero);
        var first = DisplayProfileRegistry.Reconcile(
            [],
            [Display("DISPLAY1", "stable-a"), Display("DISPLAY2", "stable-b")],
            now);

        viewModel.Refresh(first);
        var second = DisplayProfileRegistry.Reconcile(
            first.Select(record => record.Profile).ToArray(),
            [Display("DISPLAY1", "stable-a")],
            now.AddMinutes(1));
        viewModel.Refresh(second);

        Assert.Single(viewModel.ConnectedDisplays);
        Assert.Equal(2, viewModel.AllDisplays.Count);
        Assert.Contains(viewModel.AllDisplays, display =>
            display.StableId == "stable-b" && display.ConnectionStatus == "未连接");
    }

    [Fact]
    public void Refresh_ReportsRulesAssociatedByStableDisplayId()
    {
        var viewModel = new DisplayHistoryViewModel();
        var states = DisplayProfileRegistry.Reconcile(
            [],
            [Display("DISPLAY1", "stable-a"), Display("DISPLAY2", "stable-b")],
            DateTimeOffset.UtcNow);
        var rules = new[]
        {
            Rule(@"C:\Apps\one.exe", "stable-b", true),
            Rule(@"C:\Apps\two.exe", "stable-b", false),
        };

        viewModel.Refresh(states, rules);

        Assert.Equal("未配置", viewModel.AllDisplays[0].ApplicationRules);
        Assert.Equal("2 条", viewModel.AllDisplays[1].ApplicationRules);
    }

    private static ApplicationProjectionRule Rule(string path, string target, bool isEnabled) =>
        new(
            new ProjectionApplicationIdentity(ApplicationIdentityKind.ExecutablePath, path),
            Path.GetFileNameWithoutExtension(path),
            target,
            ProjectionLayout.KeepSize,
            isEnabled);

    private static DisplaySnapshot Display(string deviceName, string stableId) =>
        new(
            deviceName,
            deviceName,
            new PixelRect(0, 0, 1920, 1080),
            new PixelRect(0, 0, 1920, 1040),
            deviceName.EndsWith('1'),
            stableId);
}
