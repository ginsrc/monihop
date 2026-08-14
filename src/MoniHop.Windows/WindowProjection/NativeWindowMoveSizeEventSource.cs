using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MoniHop.Windows.WindowProjection;

public sealed class NativeWindowMoveSizeEventSource : IWindowMoveSizeEventSource
{
    private const uint EventSystemMoveSizeStart = 0x000A;
    private const uint EventSystemMoveSizeEnd = 0x000B;
    private const uint WinEventOutOfContext = 0x0000;
    private const uint WinEventSkipOwnProcess = 0x0002;

    private readonly WinEventDelegate _callback;
    private nint _hook;

    public NativeWindowMoveSizeEventSource()
    {
        _callback = OnWinEvent;
        _hook = SetWinEventHook(
            EventSystemMoveSizeStart,
            EventSystemMoveSizeEnd,
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

    public event EventHandler<WindowMoveSizeEvent>? WindowChanged;

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
        if (windowHandle == 0 || objectId != 0 || childId != 0)
        {
            return;
        }

        var kind = eventType switch
        {
            EventSystemMoveSizeStart => WindowMoveSizeEventKind.Started,
            EventSystemMoveSizeEnd => WindowMoveSizeEventKind.Ended,
            _ => (WindowMoveSizeEventKind?)null,
        };
        if (kind is not null)
        {
            WindowChanged?.Invoke(this, new WindowMoveSizeEvent(kind.Value, windowHandle));
        }
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
