using System.IO;
using System.Text.Json;
using MoniHop.Core.Displays;

namespace MoniHop.Desktop.Settings;

public sealed class JsonDisplayProfileStore : IDisplayProfileStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    public JsonDisplayProfileStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = Path.GetFullPath(filePath);
    }

    public string FilePath { get; }

    public IReadOnlyList<DisplayProfile> Load()
    {
        if (!File.Exists(FilePath))
        {
            return [];
        }

        try
        {
            using var stream = File.OpenRead(FilePath);
            return JsonSerializer.Deserialize<DisplayProfile[]>(stream, SerializerOptions) ??
                throw new JsonException("Display profile settings are empty.");
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            JsonStoreRecovery.QuarantineCorruptFile(FilePath);
            return [];
        }
    }

    public void Save(IReadOnlyList<DisplayProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        var directory = Path.GetDirectoryName(FilePath) ??
            throw new InvalidOperationException("Display profile path has no parent directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = FilePath + ".tmp";
        using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, profiles, SerializerOptions);
        }

        File.Move(temporaryPath, FilePath, overwrite: true);
    }
}
