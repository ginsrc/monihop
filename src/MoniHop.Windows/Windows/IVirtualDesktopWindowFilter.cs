namespace MoniHop.Windows.Windows;

public interface IVirtualDesktopWindowFilter
{
    bool IsOnCurrentVirtualDesktop(nint windowHandle);
}
