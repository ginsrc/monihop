using MoniHop.Desktop.Lifecycle;

namespace MoniHop.Desktop.Tests.Lifecycle;

public sealed class StartupLaunchPolicyTests
{
    [Theory]
    [InlineData(false, false, StartupLaunchAction.Continue)]
    [InlineData(false, true, StartupLaunchAction.Continue)]
    [InlineData(true, true, StartupLaunchAction.Continue)]
    [InlineData(true, false, StartupLaunchAction.RestartElevatedBeforeSingleInstance)]
    public void Decide_ElevatesBeforeAcquiringSingleInstanceWhenRequired(
        bool alwaysRunAsAdministrator,
        bool isElevated,
        StartupLaunchAction expected)
    {
        Assert.Equal(expected, StartupLaunchPolicy.Decide(alwaysRunAsAdministrator, isElevated));
    }
}
