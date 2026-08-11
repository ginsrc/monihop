using MoniHop.Core.Displays;
using MoniHop.Core.Windows;

namespace MoniHop.Windows.Windows;

public sealed class WindowSwitchService
{
    private readonly IDisplayCatalog _displayCatalog;
    private readonly IWindowController _windowController;
    private readonly WindowSwitchPlanner _planner;

    public WindowSwitchService(
        IDisplayCatalog displayCatalog,
        IWindowController windowController,
        WindowSwitchPlanner? planner = null)
    {
        _displayCatalog = displayCatalog ?? throw new ArgumentNullException(nameof(displayCatalog));
        _windowController = windowController ?? throw new ArgumentNullException(nameof(windowController));
        _planner = planner ?? new WindowSwitchPlanner();
    }

    public WindowSwitchResult Switch(
        DisplayDirection direction,
        nint excludedWindowHandle)
    {
        var windowHandle = _windowController.GetForegroundWindow();
        if (windowHandle == 0 || windowHandle == excludedWindowHandle)
        {
            return WindowSwitchResult.NoWindow;
        }

        var placement = _windowController.ReadPlacement(windowHandle);
        if (placement is null)
        {
            return WindowSwitchResult.NoWindow;
        }

        var displays = _displayCatalog.ReadAll();
        var plan = _planner.Plan(
            displays,
            placement.Value.WindowRect,
            direction);
        if (plan is null)
        {
            return WindowSwitchResult.NoTarget;
        }

        var targetNormalRect = plan.Value.TargetNormalRect;
        if (placement.Value.IsMaximized)
        {
            var normalPlan = _planner.Plan(
                displays,
                placement.Value.NormalRect,
                direction);
            if (normalPlan is not null &&
                string.Equals(
                    normalPlan.Value.TargetDisplay.DeviceName,
                    plan.Value.TargetDisplay.DeviceName,
                    StringComparison.OrdinalIgnoreCase))
            {
                targetNormalRect = normalPlan.Value.TargetNormalRect;
            }
        }

        _windowController.MoveWindow(
            windowHandle,
            placement.Value with
            {
                WindowRect = plan.Value.TargetNormalRect,
                NormalRect = targetNormalRect,
            });
        return WindowSwitchResult.Moved;
    }
}
