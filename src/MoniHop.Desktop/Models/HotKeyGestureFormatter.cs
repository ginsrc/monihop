using System.Windows.Input;
using MoniHop.Windows.HotKeys;

namespace MoniHop.Desktop.Models;

public static class HotKeyGestureFormatter
{
    public static bool TryCreate(Key key, ModifierKeys modifiers, out HotKeyGesture gesture)
    {
        gesture = default;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or
            Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            return false;
        }

        var hotKeyModifiers = HotKeyModifiers.None;
        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            hotKeyModifiers |= HotKeyModifiers.Control;
        }

        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            hotKeyModifiers |= HotKeyModifiers.Alt;
        }

        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            hotKeyModifiers |= HotKeyModifiers.Shift;
        }

        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            hotKeyModifiers |= HotKeyModifiers.Windows;
        }

        var virtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);
        if (hotKeyModifiers == HotKeyModifiers.None || virtualKey == 0)
        {
            return false;
        }

        gesture = new HotKeyGesture(hotKeyModifiers, virtualKey);
        return true;
    }

    public static string Format(HotKeyGesture? gesture)
    {
        if (gesture is null)
        {
            return "未设置";
        }

        var parts = new List<string>(5);
        var modifiers = gesture.Value.Modifiers;
        if (modifiers.HasFlag(HotKeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (modifiers.HasFlag(HotKeyModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (modifiers.HasFlag(HotKeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        if (modifiers.HasFlag(HotKeyModifiers.Windows))
        {
            parts.Add("Win");
        }

        parts.Add(FormatKey(KeyInterop.KeyFromVirtualKey((int)gesture.Value.VirtualKey)));
        return string.Join(" + ", parts);
    }

    private static string FormatKey(Key key) => key switch
    {
        >= Key.A and <= Key.Z => key.ToString(),
        >= Key.D0 and <= Key.D9 => ((int)key - (int)Key.D0).ToString(),
        Key.Left => "Left",
        Key.Right => "Right",
        Key.Up => "Up",
        Key.Down => "Down",
        Key.Space => "Space",
        Key.Enter => "Enter",
        Key.Tab => "Tab",
        _ => key.ToString(),
    };
}
