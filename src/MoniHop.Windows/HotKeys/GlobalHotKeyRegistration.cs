using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MoniHop.Windows.HotKeys;

public sealed class GlobalHotKeyRegistration : IDisposable
{
    public const int HotKeyMessage = 0x0312;
    public const int CursorSwitchId = 0x4D48;

    private const uint AltModifier = 0x0001;
    private const uint ControlModifier = 0x0002;
    private const uint NoRepeatModifier = 0x4000;
    private const uint MVirtualKey = 0x4D;

    private readonly nint _windowHandle;
    private bool _disposed;

    private GlobalHotKeyRegistration(nint windowHandle)
    {
        _windowHandle = windowHandle;
    }

    public static GlobalHotKeyRegistration Register(nint windowHandle)
    {
        if (windowHandle == 0)
        {
            throw new ArgumentException("A native window handle is required.", nameof(windowHandle));
        }

        if (!RegisterHotKey(
                windowHandle,
                CursorSwitchId,
                AltModifier | ControlModifier | NoRepeatModifier,
                MVirtualKey))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return new GlobalHotKeyRegistration(windowHandle);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _ = UnregisterHotKey(_windowHandle, CursorSwitchId);
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
