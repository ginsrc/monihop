using MoniHop.Desktop.Lifecycle;
using MoniHop.Desktop.Settings;

namespace MoniHop.Desktop.Tests.Lifecycle;

public sealed class CloseBehaviorControllerTests
{
    [Theory]
    [InlineData(CloseBehavior.MinimizeToTray, CloseAction.Hide)]
    [InlineData(CloseBehavior.Exit, CloseAction.Exit)]
    public void Resolve_ConfiguredBehavior_DoesNotPrompt(
        CloseBehavior behavior,
        CloseAction expected)
    {
        var promptCount = 0;
        var result = CloseBehaviorController.Resolve(behavior, () =>
        {
            promptCount++;
            return null;
        });

        Assert.Equal(expected, result.Action);
        Assert.Null(result.RememberedBehavior);
        Assert.Equal(0, promptCount);
    }

    [Fact]
    public void Resolve_FirstCloseRememberingTray_ReturnsPersistedChoice()
    {
        var result = CloseBehaviorController.Resolve(
            CloseBehavior.Ask,
            () => new ClosePromptResult(CloseAction.Hide, Remember: true));

        Assert.Equal(CloseAction.Hide, result.Action);
        Assert.Equal(CloseBehavior.MinimizeToTray, result.RememberedBehavior);
    }

    [Fact]
    public void Resolve_FirstCloseWithoutRemembering_LeavesAskConfigured()
    {
        var result = CloseBehaviorController.Resolve(
            CloseBehavior.Ask,
            () => new ClosePromptResult(CloseAction.Exit, Remember: false));

        Assert.Equal(CloseAction.Exit, result.Action);
        Assert.Null(result.RememberedBehavior);
    }

    [Fact]
    public void Resolve_CanceledPrompt_CancelsClose()
    {
        var result = CloseBehaviorController.Resolve(CloseBehavior.Ask, () => null);

        Assert.Equal(CloseAction.Cancel, result.Action);
        Assert.Null(result.RememberedBehavior);
    }
}
