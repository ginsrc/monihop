using MoniHop.Windows.WindowProjection;

namespace MoniHop.Windows.Tests.WindowProjection;

public sealed class WindowMoveSizeEventTests
{
    [Fact]
    public void EventStoresKindAndWindowHandle()
    {
        var value = new WindowMoveSizeEvent(WindowMoveSizeEventKind.Started, (nint)42);

        Assert.Equal(WindowMoveSizeEventKind.Started, value.Kind);
        Assert.Equal((nint)42, value.WindowHandle);
    }
}
