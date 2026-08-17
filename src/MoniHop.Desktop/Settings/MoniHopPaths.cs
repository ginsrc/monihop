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
    string DiagnosticLogFile,
    bool IsPortable)
{
    public const string PortableMarkerFileName = "portable.flag";

    public static MoniHopPaths CreateDefault() =>
        CreateForExecutableDirectory(AppContext.BaseDirectory);

    public static MoniHopPaths CreateForExecutableDirectory(string executableDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executableDirectory);
        var normalizedExecutableDirectory = Path.GetFullPath(executableDirectory);
        var isPortable = File.Exists(Path.Combine(
            normalizedExecutableDirectory,
            PortableMarkerFileName));
        var dataDirectory = isPortable
            ? Path.Combine(normalizedExecutableDirectory, "data")
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MoniHop");
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
            Path.Combine(diagnosticsDirectory, "monihop.log"),
            isPortable);
    }
}
