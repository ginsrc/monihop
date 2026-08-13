using MoniHop.Core.Displays;
using MoniHop.Desktop.Models;

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

    private static DisplaySnapshot Display(string deviceName, string stableId) =>
        new(
            deviceName,
            deviceName,
            new PixelRect(0, 0, 1920, 1080),
            new PixelRect(0, 0, 1920, 1040),
            deviceName.EndsWith('1'),
            stableId);
}
