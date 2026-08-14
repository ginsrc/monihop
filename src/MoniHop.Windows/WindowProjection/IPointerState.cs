using MoniHop.Core.Displays;

namespace MoniHop.Windows.WindowProjection;

public interface IPointerState
{
    PixelPoint ReadPosition();
}
