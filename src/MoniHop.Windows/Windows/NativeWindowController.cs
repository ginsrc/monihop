using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using MoniHop.Core.Displays;

namespace MoniHop.Windows.Windows;

public sealed class NativeWindowController : IWindowController
{
    private const uint GetWindowOwner = 4;
    private const int ExtendedStyleIndex = -20;
    private const int StyleIndex = -16;
    private const int CaptionStyle = 0x00C00000;
    private const int ToolWindowStyle = 0x00000080;
    private const int NoActivateWindowStyle = 0x08000000;
    private const uint NoSizePositionFlag = 0x0001;
    private const uint NoZOrderPositionFlag = 0x0004;
    private const uint NoActivatePositionFlag = 0x0010;

    public nint GetForegroundWindow() => GetForegroundWindowNative();

    public WindowPlacementSnapshot? ReadPlacement(nint windowHandle)
    {
        if (windowHandle == 0 || !IsWindow(windowHandle) ||
            !IsApplicationWindowCandidate(
                IsWindowVisible(windowHandle),
                IsIconic(windowHandle),
                GetWindowOwnerHandle(windowHandle),
                GetParent(windowHandle),
                GetWindowLongNative(windowHandle, StyleIndex),
                GetWindowLongNative(windowHandle, ExtendedStyleIndex),
                GetClassName(windowHandle)))
        {
            return null;
        }

        var placement = NativeWindowPlacement.Create();
        if (!GetWindowPlacement(windowHandle, ref placement))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        if (!GetWindowRect(windowHandle, out var windowRect))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return new WindowPlacementSnapshot(
            placement.ShowCommand,
            windowRect.ToPixelRect(),
            placement.NormalPosition.ToPixelRect());
    }

    public static bool IsApplicationWindowCandidate(
        bool isVisible,
        bool isIconic,
        nint owner,
        nint parent,
        int windowStyle,
        int extendedStyle,
        string className) =>
        isVisible && !isIconic && owner == 0 && parent == 0 &&
        (windowStyle & CaptionStyle) == CaptionStyle &&
        (extendedStyle & (ToolWindowStyle | NoActivateWindowStyle)) == 0 &&
        !string.Equals(className, "#32768", StringComparison.Ordinal) &&
        !string.Equals(className, "Windows.UI.Core.CoreWindow", StringComparison.OrdinalIgnoreCase);

    public void MoveWindow(nint windowHandle, WindowPlacementSnapshot placement)
    {
        if (windowHandle == 0)
        {
            throw new ArgumentException("A native window handle is required.", nameof(windowHandle));
        }

        var target = placement.WindowRect;
        var flags = NoZOrderPositionFlag | NoActivatePositionFlag;
        NativeWindowPlacement? originalPlacement = null;
        if (placement.IsMaximized)
        {
            flags |= NoSizePositionFlag;
            var nativePlacement = NativeWindowPlacement.Create();
            if (!GetWindowPlacement(windowHandle, ref nativePlacement))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            originalPlacement = nativePlacement;
            nativePlacement.NormalPosition = NativeRect.FromPixelRect(placement.NormalRect);
            if (!SetWindowPlacement(windowHandle, ref nativePlacement))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        if (!SetWindowPos(
                windowHandle,
                0,
                target.Left,
                target.Top,
                target.Width,
                target.Height,
                flags))
        {
            var error = Marshal.GetLastWin32Error();
            if (originalPlacement is not null)
            {
                var placementToRestore = originalPlacement.Value;
                _ = SetWindowPlacement(windowHandle, ref placementToRestore);
            }

            throw new Win32Exception(error);
        }
    }

    [DllImport("user32.dll", EntryPoint = "GetForegroundWindow")]
    private static extern nint GetForegroundWindowNative();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(nint windowHandle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint windowHandle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(nint windowHandle);

    [DllImport("user32.dll", EntryPoint = "GetWindow")]
    private static extern nint GetWindowOwnerHandle(nint windowHandle, uint command);

    private static nint GetWindowOwnerHandle(nint windowHandle) => GetWindowOwnerHandle(windowHandle, GetWindowOwner);

    [DllImport("user32.dll")]
    private static extern nint GetParent(nint windowHandle);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLongNative(nint windowHandle, int index);

    private static string GetClassName(nint windowHandle)
    {
        var buffer = new StringBuilder(256);
        return GetClassNameNative(windowHandle, buffer, buffer.Capacity) == 0 ? string.Empty : buffer.ToString();
    }

    [DllImport("user32.dll", EntryPoint = "GetClassNameW", CharSet = CharSet.Unicode)]
    private static extern int GetClassNameNative(nint windowHandle, StringBuilder className, int maxCount);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowPlacement(
        nint windowHandle,
        ref NativeWindowPlacement placement);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPlacement(
        nint windowHandle,
        ref NativeWindowPlacement placement);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(
        nint windowHandle,
        out NativeRect windowRect);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        nint windowHandle,
        nint insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public readonly PixelRect ToPixelRect() => new(Left, Top, Right, Bottom);

        public static NativeRect FromPixelRect(PixelRect rect) => new()
        {
            Left = rect.Left,
            Top = rect.Top,
            Right = rect.Right,
            Bottom = rect.Bottom,
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeWindowPlacement
    {
        public uint Length;
        public uint Flags;
        public uint ShowCommand;
        public NativePoint MinPosition;
        public NativePoint MaxPosition;
        public NativeRect NormalPosition;

        public static NativeWindowPlacement Create() => new()
        {
            Length = (uint)Marshal.SizeOf<NativeWindowPlacement>(),
        };

    }
}
