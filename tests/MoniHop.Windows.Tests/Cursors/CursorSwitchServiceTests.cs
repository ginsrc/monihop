using MoniHop.Core.Displays;
using MoniHop.Core.Windows;
using MoniHop.Windows.Cursors;

namespace MoniHop.Windows.Tests.Cursors;

public sealed class CursorSwitchServiceTests
{
    [Fact]
    public void SwitchNext_MovesToPlannedPosition()
    {
        var catalog = new StubDisplayCatalog(TwoDisplays());
        var cursor = new RecordingCursorController(new PixelPoint(50, 50));
        var service = new CursorSwitchService(catalog, cursor);

        var result = service.SwitchNext();

        Assert.Equal(CursorSwitchResult.Moved, result);
        Assert.Equal(new PixelPoint(150, 50), cursor.LastSetPosition);
    }

    [Fact]
    public void SwitchNext_DoesNotWriteCursorWhenNoTargetExists()
    {
        var catalog = new StubDisplayCatalog([
            Display("DISPLAY1", new PixelRect(0, 0, 100, 100))]);
        var cursor = new RecordingCursorController(new PixelPoint(50, 50));
        var service = new CursorSwitchService(catalog, cursor);

        Assert.Equal(CursorSwitchResult.NoTarget, service.SwitchNext());
        Assert.Null(cursor.LastSetPosition);
    }

    [Fact]
    public void SwitchPrevious_MovesInReverseOrder()
    {
        var cursor = new RecordingCursorController(new PixelPoint(150, 50));
        var service = new CursorSwitchService(new StubDisplayCatalog(TwoDisplays()), cursor);

        var result = service.Switch(DisplayDirection.Previous);

        Assert.Equal(CursorSwitchResult.Moved, result);
        Assert.Equal(new PixelPoint(50, 50), cursor.LastSetPosition);
    }

    [Fact]
    public void CenterCurrentDisplay_MovesToCurrentDisplayCenter()
    {
        var cursor = new RecordingCursorController(new PixelPoint(125, 25));
        var service = new CursorSwitchService(new StubDisplayCatalog(TwoDisplays()), cursor);

        var result = service.CenterCurrentDisplay();

        Assert.Equal(CursorSwitchResult.Moved, result);
        Assert.Equal(new PixelPoint(150, 50), cursor.LastSetPosition);
    }

    [Fact]
    public void MoveToCenter_UsesVisibleRectangleCenter()
    {
        var cursor = new RecordingCursorController(new PixelPoint(0, 0));
        var service = new CursorSwitchService(new StubDisplayCatalog(TwoDisplays()), cursor);

        service.MoveToCenter(new PixelRect(120, 20, 180, 80));

        Assert.Equal(new PixelPoint(150, 50), cursor.LastSetPosition);
    }

    private static IReadOnlyList<DisplaySnapshot> TwoDisplays() =>
    [
        Display("DISPLAY1", new PixelRect(0, 0, 100, 100)),
        Display("DISPLAY2", new PixelRect(100, 0, 200, 100)),
    ];

    private static DisplaySnapshot Display(string name, PixelRect bounds) =>
        new(name, name, bounds, bounds, name == "DISPLAY1");

    private sealed class StubDisplayCatalog(IReadOnlyList<DisplaySnapshot> displays) : IDisplayCatalog
    {
        public IReadOnlyList<DisplaySnapshot> ReadAll() => displays;
    }

    private sealed class RecordingCursorController(PixelPoint position) : ICursorController
    {
        public PixelPoint? LastSetPosition { get; private set; }

        public PixelPoint GetPosition() => position;

        public void SetPosition(PixelPoint position) => LastSetPosition = position;
    }
}
