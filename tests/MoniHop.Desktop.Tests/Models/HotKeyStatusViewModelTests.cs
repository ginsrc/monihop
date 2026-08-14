using MoniHop.Desktop.Models;
using MoniHop.Windows.HotKeys;

namespace MoniHop.Desktop.Tests.Models;

public sealed class HotKeyStatusViewModelTests
{
    [Fact]
    public void Update_ChangesOnlyTheTargetHotKeyStatus()
    {
        var cursor = new HotKeyStatusViewModel("鼠标切到下一屏", "Ctrl + Alt + M");
        var previous = new HotKeyStatusViewModel(
            "当前窗口移到上一屏",
            "Ctrl + Alt + Shift + Left");

        cursor.Update(isAvailable: false);

        Assert.Equal("冲突", cursor.Status);
        Assert.False(cursor.IsAvailable);
        Assert.Equal("正在注册", previous.Status);
        Assert.True(previous.IsAvailable);
    }

    [Fact]
    public void ExecutableAction_ShowsUnsetAndCanBeEdited()
    {
        var definition = HotKeyCatalog.All.Single(
            item => item.Command == HotKeyCommand.OpenProjectionPanel);

        var model = new HotKeyStatusViewModel(definition, gesture: null);

        Assert.Equal("未设置", model.Shortcut);
        Assert.Equal("未设置", model.Status);
        Assert.False(model.IsAvailable);
        Assert.True(model.CanEdit);
    }

    [Fact]
    public void DisplayAction_WithBindingAndDisconnectedTargetShowsUnavailableTarget()
    {
        var definition = HotKeyCatalog.CreateForDisplays(
            [new HotKeyDisplayTarget("stable-a", "副屏", false)])
            .Single(item => item.Command == HotKeyCommand.ProjectWindowToSpecificDisplay);

        var model = new HotKeyStatusViewModel(
            definition,
            new HotKeyGesture(HotKeyModifiers.Control | HotKeyModifiers.Alt, 0x32));

        Assert.Equal("目标未连接", model.Status);
        Assert.False(model.IsAvailable);
        Assert.True(model.CanEdit);
    }

    [Fact]
    public void SetGesture_FormatsTheShortcutAndReturnsToRegistrationState()
    {
        var definition = HotKeyCatalog.All.Single(
            item => item.Command == HotKeyCommand.CursorNext);
        var model = new HotKeyStatusViewModel(definition, gesture: null);

        model.SetGesture(new HotKeyGesture(HotKeyModifiers.Control | HotKeyModifiers.Alt, 0x50));

        Assert.Equal("Ctrl + Alt + P", model.Shortcut);
        Assert.Equal("正在注册", model.Status);
        Assert.True(model.CanEdit);
    }
}
