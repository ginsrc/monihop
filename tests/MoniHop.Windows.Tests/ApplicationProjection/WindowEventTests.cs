using MoniHop.Windows.ApplicationProjection;

namespace MoniHop.Windows.Tests.ApplicationProjection;

public sealed class WindowEventTests
{
    [Theory]
    [InlineData(WindowEventKind.Shown, 0, 0, true)]
    [InlineData(WindowEventKind.Destroyed, 0, 0, true)]
    [InlineData(WindowEventKind.Shown, -4, 0, false)]
    [InlineData(WindowEventKind.Shown, 0, 1, false)]
    public void IsWindowEvent_AcceptsOnlyWindowSelfEvents(
        WindowEventKind kind,
        int objectId,
        int childId,
        bool expected)
    {
        Assert.Equal(expected, WindowEvent.IsWindowEvent(kind, objectId, childId));
    }
}
