using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MoniHop.Windows.HotKeys;

public sealed class GlobalHotKeyRegistration : IDisposable
{
    public const int HotKeyMessage = 0x0312;
    public const int CursorSwitchId = 0x4D48;
    public const int WindowNextId = 0x4D49;
    public const int WindowPreviousId = 0x4D4A;

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
        var gesture = action switch
        {
            GlobalHotKeyAction.CursorSwitch => new HotKeyGesture(
                HotKeyModifiers.Alt | HotKeyModifiers.Control,
                MVirtualKey),
            GlobalHotKeyAction.WindowNext => new HotKeyGesture(
                HotKeyModifiers.Alt | HotKeyModifiers.Control | HotKeyModifiers.Shift,
                RightVirtualKey),
            GlobalHotKeyAction.WindowPrevious => new HotKeyGesture(
                HotKeyModifiers.Alt | HotKeyModifiers.Control | HotKeyModifiers.Shift,
                LeftVirtualKey),
            _ => throw new ArgumentOutOfRangeException(nameof(action)),
        };

        return Register(windowHandle, action, gesture);
    }

    public static GlobalHotKeyRegistration Register(
        nint windowHandle,
        GlobalHotKeyAction action,
        HotKeyGesture gesture)
        => Register(windowHandle, GetRegistrationId(action), gesture);

    public static GlobalHotKeyRegistration Register(
        nint windowHandle,
        int registrationId,
        HotKeyGesture gesture)
    {
        if (windowHandle == 0)
        {
            throw new ArgumentException("A native window handle is required.", nameof(windowHandle));
        }

        ValidateRegistrationId(registrationId);
        var modifiers = (uint)gesture.Modifiers | NoRepeatModifier;

        if (!RegisterHotKey(windowHandle, registrationId, modifiers, gesture.VirtualKey))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return new GlobalHotKeyRegistration(windowHandle, registrationId);
    }

    public static void ValidateRegistrationId(int registrationId)
    {
        if (registrationId is < 1 or > 0xBFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(registrationId));
        }
    }

    public static int GetRegistrationId(GlobalHotKeyAction action) => action switch
    {
        GlobalHotKeyAction.CursorSwitch => CursorSwitchId,
        GlobalHotKeyAction.WindowNext => WindowNextId,
        GlobalHotKeyAction.WindowPrevious => WindowPreviousId,
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };

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
