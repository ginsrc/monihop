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

    [Fact]
    public void TryCreate_UsesPhysicalWindowsKeyStateForOemKey()
    {
        var created = HotKeyGestureFormatter.TryCreate(
            Key.Oem3,
            ModifierKeys.None,
            windowsKeyDown: true,
            out var gesture);

        Assert.True(created);
        Assert.Equal(
            new HotKeyGesture(HotKeyModifiers.Windows, 0xC0),
            gesture);
        Assert.Equal("Win + `", HotKeyGestureFormatter.Format(gesture));
    }

    [Fact]
    public void ResolveKey_UnwrapsImeProcessedKey()
    {
        var resolved = HotKeyGestureFormatter.ResolveKey(
            Key.ImeProcessed,
            Key.None,
            Key.Oem3,
            Key.None);

        Assert.Equal(Key.Oem3, resolved);
    }

    [Fact]
    public void ResolveKey_UsesOriginalVirtualKeyWhenImeHidesThePhysicalKey()
    {
        var resolved = HotKeyGestureFormatter.ResolveKey(
            Key.ImeProcessed,
            Key.None,
            Key.ImeProcessed,
            Key.None,
            originalImeVirtualKey: 0xC0);

        Assert.Equal(Key.Oem3, resolved);
    }

    [Fact]
    public void TryCreate_RejectsImeWrapperKeys()
    {
        Assert.False(HotKeyGestureFormatter.TryCreate(
            Key.ImeProcessed,
            ModifierKeys.Windows,
            out _));
    }
}
