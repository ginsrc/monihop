using System.IO;

namespace MoniHop.Desktop.Settings;

public sealed record MoniHopPaths(
    string DataDirectory,
    string ConfigurationDirectory,
    string DiagnosticsDirectory,
    string DisplayProfilesFile,
    string ApplicationProjectionFile,
    string WindowProjectionFile,
    string HotKeysFile,
    string GeneralSettingsFile,
    string DiagnosticLogFile)
{
    public static MoniHopPaths CreateDefault()
    {
        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dataDirectory = Path.Combine(localData, "MoniHop");
        var configurationDirectory = Path.Combine(dataDirectory, "config");
        var diagnosticsDirectory = Path.Combine(dataDirectory, "diagnostics");
        return new MoniHopPaths(
            dataDirectory,
            configurationDirectory,
            diagnosticsDirectory,
            Path.Combine(configurationDirectory, "display-profiles.json"),
            Path.Combine(configurationDirectory, "application-projection.json"),
            Path.Combine(configurationDirectory, "window-projection.json"),
            Path.Combine(configurationDirectory, "hotkeys.json"),
            Path.Combine(configurationDirectory, "general.json"),
            Path.Combine(diagnosticsDirectory, "monihop.log"));
    }
}
