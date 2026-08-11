using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MoniHop.Windows.HotKeys;

public sealed class GlobalHotKeyRegistration : IDisposable
{
    public const int HotKeyMessage = 0x0312;
    public const int CursorSwitchId = 0x4D48;
    public const int WindowNextId = 0x4D49;
    public const int WindowPreviousId = 0x4D4A;

    private const uint AltModifier = 0x0001;
    private const uint ControlModifier = 0x0002;
    private const uint ShiftModifier = 0x0004;
    private const uint NoRepeatModifier = 0x4000;
    private const uint MVirtualKey = 0x4D;
    private const uint LeftVirtualKey = 0x25;
    private const uint RightVirtualKey = 0x27;

    private readonly nint _windowHandle;
    private readonly int _id;
    private bool _disposed;

    private GlobalHotKeyRegistration(nint windowHandle, int id)
    {
        _windowHandle = windowHandle;
        _id = id;
    }

    public static GlobalHotKeyRegistration Register(nint windowHandle) =>
        Register(windowHandle, GlobalHotKeyAction.CursorSwitch);

    public static GlobalHotKeyRegistration Register(
        nint windowHandle,
        GlobalHotKeyAction action)
    {
        if (windowHandle == 0)
        {
            throw new ArgumentException("A native window handle is required.", nameof(windowHandle));
        }

        var (id, modifiers, virtualKey) = action switch
        {
            GlobalHotKeyAction.CursorSwitch => (
                CursorSwitchId,
                AltModifier | ControlModifier | NoRepeatModifier,
                MVirtualKey),
            GlobalHotKeyAction.WindowNext => (
                WindowNextId,
                AltModifier | ControlModifier | ShiftModifier | NoRepeatModifier,
                RightVirtualKey),
            GlobalHotKeyAction.WindowPrevious => (
                WindowPreviousId,
                AltModifier | ControlModifier | ShiftModifier | NoRepeatModifier,
                LeftVirtualKey),
            _ => throw new ArgumentOutOfRangeException(nameof(action)),
        };

        if (!RegisterHotKey(windowHandle, id, modifiers, virtualKey))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return new GlobalHotKeyRegistration(windowHandle, id);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _ = UnregisterHotKey(_windowHandle, _id);
        _disposed = true;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(
        nint windowHandle,
        int id,
        uint modifiers,
        uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(nint windowHandle, int id);
}
