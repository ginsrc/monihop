using System.Runtime.InteropServices;

namespace MoniHop.Windows.HotKeys;

public static class WindowsLogoKeyState
{
    private const int LeftWindowsVirtualKey = 0x5B;
    private const int RightWindowsVirtualKey = 0x5C;
    private const int KeyPressedMask = 0x8000;

    public static bool IsPressed() =>
        IsPressed(LeftWindowsVirtualKey) || IsPressed(RightWindowsVirtualKey);

    private static bool IsPressed(int virtualKey) =>
        (GetAsyncKeyState(virtualKey) & KeyPressedMask) != 0;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);
}
