using System.ComponentModel;
using MoniHop.Core.ApplicationProjection;
using MoniHop.Core.Displays;
using MoniHop.Windows.ApplicationProjection;

namespace MoniHop.Windows.Windows;

public sealed class OffscreenWindowRecallService
{
    private readonly IDisplayCatalog _displayCatalog;
    private readonly IApplicationWindowController _windowController;
    private readonly IVirtualDesktopWindowFilter _virtualDesktopFilter;

    public OffscreenWindowRecallService(
        IDisplayCatalog displayCatalog,
        IApplicationWindowController windowController,
        IVirtualDesktopWindowFilter virtualDesktopFilter)
    {
        _displayCatalog = displayCatalog ?? throw new ArgumentNullException(nameof(displayCatalog));
        _windowController = windowController ?? throw new ArgumentNullException(nameof(windowController));
        _virtualDesktopFilter = virtualDesktopFilter ?? throw new ArgumentNullException(nameof(virtualDesktopFilter));
    }

    public OffscreenWindowRecallResult Recall(nint excludedWindowHandle)
    {
        var displays = _displayCatalog.ReadAll();
        if (displays.Count == 0)
        {
            return new OffscreenWindowRecallResult(OffscreenWindowRecallStatus.NoDisplay, 0, 0, 0);
        }

        var target = displays.FirstOrDefault(display => display.IsPrimary) ?? displays[0];
        var moved = 0;
        var failed = 0;
        var skippedOtherDesktop = 0;
        foreach (var window in _windowController.ReadAllWindows())
        {
            if (window.WindowHandle == excludedWindowHandle || IntersectsAnyDisplay(window.Placement.WindowRect, displays))
            {
                continue;
            }

            if (!_virtualDesktopFilter.IsOnCurrentVirtualDesktop(window.WindowHandle))
            {
                skippedOtherDesktop++;
                continue;
            }

            try
            {
                var sourceRect = window.Placement.IsMaximized
                    ? window.Placement.NormalRect
                    : window.Placement.WindowRect;
                var targetRect = ProjectionGeometry.CalculateTargetRect(
                    displays,
                    target,
                    sourceRect,
                    ProjectionLayout.KeepSize);
                _windowController.Move(window.WindowHandle, new ApplicationProjectionPlan(
                    target,
                    targetRect,
                    ProjectionLayout.KeepSize,
                    false,
                    ProjectionRuleSource.Global,
                    false));
                moved++;
            }
            catch (Exception exception) when (
                exception is Win32Exception or UnauthorizedAccessException or ArgumentException)
            {
                failed++;
            }
        }

        return new OffscreenWindowRecallResult(
            OffscreenWindowRecallStatus.Completed,
            moved,
            failed,
            skippedOtherDesktop);
    }

    private static bool IntersectsAnyDisplay(
        PixelRect rect,
        IReadOnlyList<DisplaySnapshot> displays) =>
        displays.Any(display =>
            rect.Left < display.WorkingArea.Right && rect.Right > display.WorkingArea.Left &&
            rect.Top < display.WorkingArea.Bottom && rect.Bottom > display.WorkingArea.Top);
}

public sealed record OffscreenWindowRecallResult(
    OffscreenWindowRecallStatus Status,
    int MovedCount,
    int FailedCount,
    int SkippedOtherDesktopCount);

public enum OffscreenWindowRecallStatus
{
    Completed,
    NoDisplay,
}
