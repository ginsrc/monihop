using MoniHop.Core.ApplicationProjection;

namespace MoniHop.Desktop.Settings;

public interface IApplicationProjectionStore
{
    ApplicationProjectionSettings Load();

    void Save(ApplicationProjectionSettings settings);
}
