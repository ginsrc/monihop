using System.Runtime.InteropServices;

namespace MoniHop.Windows.HotKeys;

public static class NativeImeKeyResolver
{
    public static uint GetOriginalVirtualKey(nint windowHandle) =>
        windowHandle == 0 ? 0 : ImmGetVirtualKey(windowHandle);

    [DllImport("imm32.dll")]
    private static extern uint ImmGetVirtualKey(nint windowHandle);
}
