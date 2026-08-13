using MoniHop.Core.Displays;

namespace MoniHop.Desktop.Settings;

public interface IDisplayProfileStore
{
    string FilePath { get; }

    IReadOnlyList<DisplayProfile> Load();

    void Save(IReadOnlyList<DisplayProfile> profiles);
}
