using MoniHop.Core.ApplicationProjection;
using MoniHop.Core.Displays;
using MoniHop.Core.WindowProjection;

namespace MoniHop.Desktop.WindowProjection;

public interface IWindowProjectionOverlay : IDisposable
{
    void ShowPortal(PixelRect bounds);

    void ShowCommands(
        WindowProjectionCommandLayout command,
        ProjectionLayout defaultLayout,
        WindowProjectionHit hit);

    void UpdateCommands(
        WindowProjectionCommandLayout command,
        ProjectionLayout defaultLayout,
        WindowProjectionHit hit);

    void Hide();
}
