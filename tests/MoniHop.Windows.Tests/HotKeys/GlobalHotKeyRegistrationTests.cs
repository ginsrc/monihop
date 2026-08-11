using MoniHop.Windows.HotKeys;

namespace MoniHop.Windows.Tests.HotKeys;

public sealed class GlobalHotKeyRegistrationTests
{
    [Fact]
    public void ActionIds_AreUnique()
    {
        var actionIds = new[]
        {
            GlobalHotKeyRegistration.CursorSwitchId,
            GlobalHotKeyRegistration.WindowNextId,
            GlobalHotKeyRegistration.WindowPreviousId,
        };

        Assert.Equal(actionIds.Length, actionIds.Distinct().Count());
    }

    [Fact]
    public void Register_RejectsMissingWindowHandle()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => GlobalHotKeyRegistration.Register(0, GlobalHotKeyAction.CursorSwitch));

        Assert.Equal("windowHandle", exception.ParamName);
    }
}
