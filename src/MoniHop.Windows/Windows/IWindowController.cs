namespace MoniHop.Windows.Windows;

public interface IWindowController
{
    nint GetForegroundWindow();

    WindowPlacementSnapshot? ReadPlacement(nint windowHandle);

    WindowCapabilities ReadCapabilities(nint windowHandle) => WindowCapabilities.Standard;

    void MoveWindow(nint windowHandle, WindowPlacementSnapshot placement);
}
