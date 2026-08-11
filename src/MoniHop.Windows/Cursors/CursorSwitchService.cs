using MoniHop.Core.Cursors;
using MoniHop.Core.Displays;

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

    public CursorSwitchResult SwitchNext()
    {
        var displays = _displayCatalog.ReadAll();
        var currentPosition = _cursorController.GetPosition();
        var targetPosition = _planner.PlanNext(displays, currentPosition);

        if (targetPosition is null)
        {
            return CursorSwitchResult.NoTarget;
        }

        _cursorController.SetPosition(targetPosition.Value);
        return CursorSwitchResult.Moved;
    }
}
