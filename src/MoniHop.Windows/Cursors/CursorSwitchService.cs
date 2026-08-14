using MoniHop.Core.Cursors;
using MoniHop.Core.Displays;
using MoniHop.Core.Windows;

namespace MoniHop.Windows.Cursors;

public sealed class CursorSwitchService
{
    private readonly IDisplayCatalog _displayCatalog;
    private readonly ICursorController _cursorController;
    private readonly CursorSwitchPlanner _planner;

    public CursorSwitchService(
        IDisplayCatalog displayCatalog,
        ICursorController cursorController,
        CursorSwitchPlanner? planner = null)
    {
        _displayCatalog = displayCatalog ?? throw new ArgumentNullException(nameof(displayCatalog));
        _cursorController = cursorController ?? throw new ArgumentNullException(nameof(cursorController));
        _planner = planner ?? new CursorSwitchPlanner();
    }

    public CursorSwitchResult SwitchNext() => Switch(DisplayDirection.Next);

    public CursorSwitchResult Switch(DisplayDirection direction)
    {
        var displays = _displayCatalog.ReadAll();
        var currentPosition = _cursorController.GetPosition();
        var targetPosition = _planner.Plan(displays, currentPosition, direction);

        if (targetPosition is null)
        {
            return CursorSwitchResult.NoTarget;
        }

        _cursorController.SetPosition(targetPosition.Value);
        return CursorSwitchResult.Moved;
    }

    public CursorSwitchResult CenterCurrentDisplay()
    {
        var targetPosition = _planner.PlanCenter(
            _displayCatalog.ReadAll(),
            _cursorController.GetPosition());
        if (targetPosition is null)
        {
            return CursorSwitchResult.NoTarget;
        }

        _cursorController.SetPosition(targetPosition.Value);
        return CursorSwitchResult.Moved;
    }

    public void MoveToCenter(PixelRect rect)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rect));
        }

        _cursorController.SetPosition(new PixelPoint(
            rect.Left + (rect.Width / 2),
            rect.Top + (rect.Height / 2)));
    }
}
