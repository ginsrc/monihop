using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MoniHop.Windows.Windows;

 [SupportedOSPlatform("windows")]
public sealed class NativeVirtualDesktopWindowFilter : IVirtualDesktopWindowFilter
{
    private static readonly Guid VirtualDesktopManagerClassId =
        new("AA509086-5CA9-4C25-8F95-589D3C07B48A");
    private IVirtualDesktopManager? _manager;

    public bool IsOnCurrentVirtualDesktop(nint windowHandle)
    {
        if (windowHandle == 0)
        {
            return false;
        }

        try
        {
            _manager ??= CreateManager();
            return _manager.IsWindowOnCurrentVirtualDesktop(windowHandle, out var isCurrent) == 0 && isCurrent;
        }
        catch (Exception exception) when (exception is COMException or InvalidCastException)
        {
            return false;
        }
    }

    private static IVirtualDesktopManager CreateManager()
    {
        var type = Type.GetTypeFromCLSID(VirtualDesktopManagerClassId, throwOnError: true) ??
            throw new COMException("Virtual Desktop Manager is unavailable.");
        return (IVirtualDesktopManager)(Activator.CreateInstance(type) ??
            throw new COMException("Virtual Desktop Manager could not be created."));
    }

    [ComImport]
    [Guid("A5CD92FF-29BE-454C-8D04-D82879FB3F1B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IVirtualDesktopManager
    {
        [PreserveSig]
        int IsWindowOnCurrentVirtualDesktop(nint topLevelWindow, [MarshalAs(UnmanagedType.Bool)] out bool isCurrent);

        [PreserveSig]
        int GetWindowDesktopId(nint topLevelWindow, out Guid desktopId);

        [PreserveSig]
        int MoveWindowToDesktop(nint topLevelWindow, [In] ref Guid desktopId);
    }
}
