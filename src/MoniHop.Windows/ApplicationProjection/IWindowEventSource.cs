namespace MoniHop.Windows.ApplicationProjection;

public interface IWindowEventSource : IDisposable
{
    event EventHandler<WindowEvent>? WindowChanged;
}
