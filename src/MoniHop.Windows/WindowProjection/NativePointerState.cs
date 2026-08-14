using System.ComponentModel;
using System.Runtime.InteropServices;
using MoniHop.Core.Displays;

namespace MoniHop.Windows.WindowProjection;

public sealed class NativePointerState : IPointerState
{
    public PixelPoint ReadPosition()
    {
        if (!GetCursorPos(out var point))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return new PixelPoint(point.X, point.Y);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint point);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }
}
