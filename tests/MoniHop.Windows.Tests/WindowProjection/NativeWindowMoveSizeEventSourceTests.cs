using MoniHop.Windows.WindowProjection;

namespace MoniHop.Windows.Tests.WindowProjection;

public sealed class NativeWindowMoveSizeEventSourceTests
{
    [Fact]
    public void Dispose_CanBeCalledMoreThanOnce()
    {
        var source = new NativeWindowMoveSizeEventSource();

        source.Dispose();
        source.Dispose();
    }
}
