using System.IO;
using System.Text.Json;
using MoniHop.Desktop.Models;
using MoniHop.Windows.HotKeys;

namespace MoniHop.Desktop.Settings;

public sealed class JsonHotKeyStore : IHotKeyStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public JsonHotKeyStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = Path.GetFullPath(filePath);
    }

    public string FilePath { get; }

    public HotKeySettings Load()
    {
        if (!File.Exists(FilePath))
        {
            return HotKeySettings.CreateDefaults();
        }

        try
        {
            using var stream = File.OpenRead(FilePath);
            using var document = JsonDocument.Parse(stream);
            if (document.RootElement.TryGetProperty(nameof(HotKeySettings.Bindings), out _))
            {
                var current = document.RootElement.Deserialize<HotKeySettingsDocument>(SerializerOptions);
                return new HotKeySettings(current?.Bindings);
            }

            var legacy = document.RootElement.Deserialize<LegacyHotKeySettings>(SerializerOptions);
            var settings = HotKeySettings.CreateDefaults();
            settings = settings.With(HotKeyCatalog.Get(HotKeyCommand.CursorNext).Id, legacy?.CursorNext);
            settings = settings.With(HotKeyCatalog.Get(HotKeyCommand.WindowPrevious).Id, legacy?.WindowPrevious);
            return settings.With(HotKeyCatalog.Get(HotKeyCommand.WindowNext).Id, legacy?.WindowNext);
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            return HotKeySettings.CreateDefaults();
        }
    }

    public void Save(HotKeySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var directory = Path.GetDirectoryName(FilePath) ??
            throw new InvalidOperationException("Hot key settings path has no parent directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = FilePath + ".tmp";
        using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(
                stream,
                new HotKeySettingsDocument(new Dictionary<string, HotKeyGesture>(settings.Bindings)),
                SerializerOptions);
        }

        File.Move(temporaryPath, FilePath, overwrite: true);
    }

    private sealed record HotKeySettingsDocument(Dictionary<string, HotKeyGesture>? Bindings);

    private sealed record LegacyHotKeySettings(
        HotKeyGesture? CursorNext,
        HotKeyGesture? WindowPrevious,
        HotKeyGesture? WindowNext);
}
