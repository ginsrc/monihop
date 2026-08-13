using MoniHop.Desktop.Models;

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
}
