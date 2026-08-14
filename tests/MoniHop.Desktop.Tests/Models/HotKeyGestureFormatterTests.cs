using System.Windows.Input;
using MoniHop.Desktop.Models;
using MoniHop.Windows.HotKeys;

namespace MoniHop.Desktop.Tests.Models;

public sealed class HotKeyGestureFormatterTests
{
    [Fact]
    public void TryCreate_RejectsPlainKeysAndModifierKeys()
    {
        Assert.False(HotKeyGestureFormatter.TryCreate(Key.P, ModifierKeys.None, out _));
        Assert.False(HotKeyGestureFormatter.TryCreate(Key.LeftCtrl, ModifierKeys.Control, out _));
    }

    [Fact]
    public void TryCreate_NormalizesAValidCombination()
    {
        var created = HotKeyGestureFormatter.TryCreate(
            Key.P,
            ModifierKeys.Control | ModifierKeys.Alt,
            out var gesture);

        Assert.True(created);
        Assert.Equal(
            new HotKeyGesture(HotKeyModifiers.Control | HotKeyModifiers.Alt, 0x50),
            gesture);
        Assert.Equal("Ctrl + Alt + P", HotKeyGestureFormatter.Format(gesture));
    }
}
