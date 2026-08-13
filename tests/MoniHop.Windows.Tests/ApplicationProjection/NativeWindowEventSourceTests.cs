using MoniHop.Windows.ApplicationProjection;

namespace MoniHop.Windows.Tests.ApplicationProjection;

public sealed class NativeWindowEventSourceTests
{
    [Fact]
    public void Dispose_CanBeCalledMoreThanOnce()
    {
        var source = new NativeWindowEventSource();

        source.Dispose();
        source.Dispose();
    }
}
