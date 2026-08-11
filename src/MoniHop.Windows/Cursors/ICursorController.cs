using MoniHop.Core.Displays;

namespace MoniHop.Windows.Cursors;

public interface ICursorController
{
    PixelPoint GetPosition();

    void SetPosition(PixelPoint position);
}
