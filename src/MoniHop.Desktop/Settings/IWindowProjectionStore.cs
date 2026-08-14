using MoniHop.Core.WindowProjection;

namespace MoniHop.Desktop.Settings;

public interface IWindowProjectionStore
{
    WindowProjectionSettings Load();

    void Save(WindowProjectionSettings settings);
}
