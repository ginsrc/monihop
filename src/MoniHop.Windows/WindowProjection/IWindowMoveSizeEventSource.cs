namespace MoniHop.Windows.WindowProjection;

public interface IWindowMoveSizeEventSource : IDisposable
{
    event EventHandler<WindowMoveSizeEvent>? WindowChanged;
}

public readonly record struct WindowMoveSizeEvent(WindowMoveSizeEventKind Kind, nint WindowHandle);

public enum WindowMoveSizeEventKind
{
    Started,
    Ended,
}
