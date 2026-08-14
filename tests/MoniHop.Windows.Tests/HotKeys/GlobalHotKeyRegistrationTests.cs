using MoniHop.Windows.HotKeys;

namespace MoniHop.Windows.Tests.HotKeys;

public sealed class GlobalHotKeyRegistrationTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(0xBFFF)]
    public void ValidateRegistrationId_AcceptsApplicationRange(int id)
    {
        GlobalHotKeyRegistration.ValidateRegistrationId(id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0xC000)]
    public void ValidateRegistrationId_RejectsReservedOrInvalidIds(int id)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => GlobalHotKeyRegistration.ValidateRegistrationId(id));
    }
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

    [Fact]
    public void HotKeyGesture_RejectsMissingModifierOrKey()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new HotKeyGesture(HotKeyModifiers.None, 0x4D));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new HotKeyGesture(HotKeyModifiers.Control, 0));
    }

    [Fact]
    public void ActionIds_ReturnExistingStableRegistrationIds()
    {
        Assert.Equal(
            GlobalHotKeyRegistration.CursorSwitchId,
            GlobalHotKeyRegistration.GetRegistrationId(GlobalHotKeyAction.CursorSwitch));
        Assert.Equal(
            GlobalHotKeyRegistration.WindowPreviousId,
            GlobalHotKeyRegistration.GetRegistrationId(GlobalHotKeyAction.WindowPrevious));
        Assert.Equal(
            GlobalHotKeyRegistration.WindowNextId,
            GlobalHotKeyRegistration.GetRegistrationId(GlobalHotKeyAction.WindowNext));
    }
}
