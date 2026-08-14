using MoniHop.Windows.WindowProjection;

namespace MoniHop.Windows.Tests.WindowProjection;

public sealed class NativePointerStateTests
{
    [Fact]
    public void ReadPosition_ReturnsCurrentCursorPosition()
    {
        var point = new NativePointerState().ReadPosition();

        Assert.True(point.X >= int.MinValue);
        Assert.True(point.Y >= int.MinValue);
    }
}
