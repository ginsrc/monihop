using System.IO;

namespace MoniHop.Desktop.Settings;

public sealed record MoniHopPaths(
    string DataDirectory,
    string ConfigurationDirectory,
    string DisplayProfilesFile,
    string ApplicationProjectionFile,
    string WindowProjectionFile,
    string HotKeysFile)
{
    public static MoniHopPaths CreateDefault()
    {
        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dataDirectory = Path.Combine(localData, "MoniHop");
        var configurationDirectory = Path.Combine(dataDirectory, "config");
        return new MoniHopPaths(
            dataDirectory,
            configurationDirectory,
            Path.Combine(configurationDirectory, "display-profiles.json"),
            Path.Combine(configurationDirectory, "application-projection.json"),
            Path.Combine(configurationDirectory, "window-projection.json"),
            Path.Combine(configurationDirectory, "hotkeys.json"));
    }
}
