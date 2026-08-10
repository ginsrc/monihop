using System.ComponentModel;
using System.Runtime.InteropServices;
using MoniHop.Core.Displays;

namespace MoniHop.Windows.Displays;

public sealed class NativeDisplayCatalog : IDisplayCatalog
{
    private const uint MonitorInfoPrimary = 0x00000001;

    public IReadOnlyList<DisplaySnapshot> ReadAll()
    {
        var displays = new List<DisplaySnapshot>();

        var succeeded = EnumDisplayMonitors(
            0,
            0,
            (monitor, _, _, _) =>
            {
                var info = MonitorInfoEx.Create();
                if (!GetMonitorInfo(monitor, ref info))
                {
                    return false;
                }

                var ordinal = displays.Count + 1;
                displays.Add(new DisplaySnapshot(
                    info.DeviceName,
                    $"Display {ordinal}",
                    info.Monitor.ToPixelRect(),
                    info.WorkArea.ToPixelRect(),
                    (info.Flags & MonitorInfoPrimary) != 0));

                return true;
            },
            0);

        if (!succeeded)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return displays.AsReadOnly();
    }

    private delegate bool MonitorEnumProc(
        nint monitor,
        nint deviceContext,
        nint monitorRect,
        nint data);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(
        nint deviceContext,
        nint clipRect,
        MonitorEnumProc callback,
        nint data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfoEx monitorInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public readonly PixelRect ToPixelRect() => new(Left, Top, Right, Bottom);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfoEx
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        public static MonitorInfoEx Create() => new()
        {
            Size = Marshal.SizeOf<MonitorInfoEx>(),
            DeviceName = string.Empty,
        };
    }
}
