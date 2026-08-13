using System.IO;
using System.Text.Json;
using MoniHop.Core.ApplicationProjection;

namespace MoniHop.Desktop.Settings;

public sealed class JsonApplicationProjectionStore : IApplicationProjectionStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    public JsonApplicationProjectionStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = Path.GetFullPath(filePath);
    }

    public string FilePath { get; }

    public ApplicationProjectionSettings Load()
    {
        if (!File.Exists(FilePath))
        {
            return ApplicationProjectionSettings.Default;
        }

        using var stream = File.OpenRead(FilePath);
        return JsonSerializer.Deserialize<ApplicationProjectionSettings>(stream, SerializerOptions) ??
            ApplicationProjectionSettings.Default;
    }

    public void Save(ApplicationProjectionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var directory = Path.GetDirectoryName(FilePath) ??
            throw new InvalidOperationException("Application projection path has no parent directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = FilePath + ".tmp";
        using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, settings, SerializerOptions);
        }

        File.Move(temporaryPath, FilePath, overwrite: true);
    }
}
