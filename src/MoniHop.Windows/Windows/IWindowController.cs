namespace MoniHop.Windows.Windows;

public interface IWindowController
{
    nint GetForegroundWindow();

    WindowPlacementSnapshot? ReadPlacement(nint windowHandle);

    void MoveWindow(nint windowHandle, WindowPlacementSnapshot placement);
}
