using MoniHop.Desktop.Lifecycle;

namespace MoniHop.Desktop.Tests.Lifecycle;

public sealed class SingleInstanceCoordinatorTests
{
    [Fact]
    public async Task SecondaryInstance_SignalsPrimaryToShowSettings()
    {
        var name = "MoniHop.Tests." + Guid.NewGuid().ToString("N");
        using var primary = new SingleInstanceCoordinator(name);
        using var secondary = new SingleInstanceCoordinator(name);
        var requested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        primary.ShowRequested += (_, _) => requested.TrySetResult();

        Assert.True(primary.IsPrimary);
        Assert.False(secondary.IsPrimary);

        await secondary.SignalPrimaryAsync();

        await requested.Task.WaitAsync(TimeSpan.FromSeconds(3));
    }
}
