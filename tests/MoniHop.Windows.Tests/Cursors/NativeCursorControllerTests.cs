using MoniHop.Windows.Cursors;

namespace MoniHop.Windows.Tests.Cursors;

public sealed class NativeCursorControllerTests
{
    [Fact]
    public void GetAndSetPosition_RoundTripsCurrentPosition()
    {
        var controller = new NativeCursorController();
        var original = controller.GetPosition();

        controller.SetPosition(original);

        Assert.Equal(original, controller.GetPosition());
    }
}
