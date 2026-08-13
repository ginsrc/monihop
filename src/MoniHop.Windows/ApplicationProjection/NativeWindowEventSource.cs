using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MoniHop.Windows.ApplicationProjection;

public sealed class NativeWindowEventSource : IWindowEventSource
{
    private const uint EventObjectDestroy = 0x8001;
    private const uint EventObjectShow = 0x8002;
    private const uint WinEventOutOfContext = 0x0000;
    private const uint WinEventSkipOwnProcess = 0x0002;

    private readonly WinEventDelegate _callback;
    private nint _hook;

    public NativeWindowEventSource()
    {
        _callback = OnWinEvent;
        _hook = SetWinEventHook(
            EventObjectDestroy,
            EventObjectShow,
            0,
            _callback,
            0,
            0,
            WinEventOutOfContext | WinEventSkipOwnProcess);
        if (_hook == 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public event EventHandler<WindowEvent>? WindowChanged;

    public void Dispose()
    {
        var hook = Interlocked.Exchange(ref _hook, 0);
        if (hook != 0)
        {
            _ = UnhookWinEvent(hook);
        }

        GC.SuppressFinalize(this);
    }

    private void OnWinEvent(
        nint hook,
        uint eventType,
        nint windowHandle,
        int objectId,
        int childId,
        uint threadId,
        uint eventTime)
    {
        var kind = eventType switch
        {
            EventObjectShow => WindowEventKind.Shown,
            EventObjectDestroy => WindowEventKind.Destroyed,
            _ => (WindowEventKind?)null,
        };
        if (windowHandle == 0 ||
            kind is null ||
            !WindowEvent.IsWindowEvent(kind.Value, objectId, childId))
        {
            return;
        }

        WindowChanged?.Invoke(this, new WindowEvent(kind.Value, windowHandle));
    }

    private delegate void WinEventDelegate(
        nint hook,
        uint eventType,
        nint windowHandle,
        int objectId,
        int childId,
        uint threadId,
        uint eventTime);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWinEventHook(
        uint eventMin,
        uint eventMax,
        nint eventHookModule,
        WinEventDelegate callback,
        uint processId,
        uint threadId,
        uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWinEvent(nint hook);
}
