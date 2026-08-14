using System.IO;
using System.Text.Json;
using MoniHop.Core.WindowProjection;

namespace MoniHop.Desktop.Settings;

public sealed class JsonWindowProjectionStore : IWindowProjectionStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public JsonWindowProjectionStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = Path.GetFullPath(filePath);
    }

    public string FilePath { get; }

    public WindowProjectionSettings Load()
    {
        if (!File.Exists(FilePath))
        {
            return WindowProjectionSettings.Default;
        }

        try
        {
            using var stream = File.OpenRead(FilePath);
            var settings = JsonSerializer.Deserialize<WindowProjectionSettings>(stream, SerializerOptions) ??
                WindowProjectionSettings.Default;
            return settings.RelativePosition == new RelativePosition(0.72, 0.08)
                ? new WindowProjectionSettings(
                    settings.IsEnabled,
                    settings.TriggerMode,
                    settings.IsPositionLocked,
                    WindowProjectionSettings.Default.RelativePosition,
                    settings.DefaultTargetDisplayId,
                    settings.DefaultLayout)
                : settings;
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            return WindowProjectionSettings.Default;
        }
    }

    public void Save(WindowProjectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var directory = Path.GetDirectoryName(FilePath) ?? throw new InvalidOperationException("Window projection path has no parent directory.");
        Directory.CreateDirectory(directory);
        var temporaryPath = FilePath + ".tmp";
        using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, settings, SerializerOptions);
        }

        File.Move(temporaryPath, FilePath, true);
    }
}
