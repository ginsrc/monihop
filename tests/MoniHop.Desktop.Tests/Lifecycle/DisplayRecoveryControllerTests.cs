using MoniHop.Desktop.Lifecycle;

namespace MoniHop.Desktop.Tests.Lifecycle;

public sealed class DisplayRecoveryControllerTests
{
    [Fact]
    public void DisplaysChanged_WhenRecoveryDisabled_DoesNotRecall()
    {
        var callCount = 0;

        DisplayRecoveryController.HandleDisplaysChanged(false, () => callCount++);

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void DisplaysChanged_WhenRecoveryEnabled_RecallsOnce()
    {
        var callCount = 0;

        DisplayRecoveryController.HandleDisplaysChanged(true, () => callCount++);

        Assert.Equal(1, callCount);
    }
}
