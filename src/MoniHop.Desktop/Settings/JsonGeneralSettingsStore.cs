using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MoniHop.Core.Cursors;

namespace MoniHop.Desktop.Settings;

public sealed class JsonGeneralSettingsStore(string path) : IGeneralSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public GeneralSettings Load()
    {
        if (!File.Exists(path))
        {
            return GeneralSettings.CreateDefaults();
        }

        try
        {
            var persisted = JsonSerializer.Deserialize<PersistedGeneralSettings>(
                File.ReadAllText(path),
                SerializerOptions);
            return persisted?.ToSettings() ?? throw new JsonException("General settings are empty.");
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            JsonStoreRecovery.QuarantineCorruptFile(path);
            return GeneralSettings.CreateDefaults();
        }
    }

    public void Save(GeneralSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = path + ".tmp";
        try
        {
            File.WriteAllText(
                temporaryPath,
                JsonSerializer.Serialize(PersistedGeneralSettings.From(settings), SerializerOptions));
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private sealed record PersistedGeneralSettings(
        bool? StartWithWindows,
        CloseBehavior? CloseBehavior,
        CursorLandingMode? CursorLanding,
        bool? RecallOffscreenWindows,
        bool? ShowSuccessNotifications,
        bool? AlwaysRunAsAdministrator,
        bool? CheckForUpdatesAutomatically,
        bool? DetailedDiagnosticsEnabled,
        AppTheme? Theme,
        AppLanguage? Language)
    {
        public static PersistedGeneralSettings From(GeneralSettings settings) => new(
            settings.StartWithWindows,
            settings.CloseBehavior,
            settings.CursorLanding,
            settings.RecallOffscreenWindows,
            settings.ShowSuccessNotifications,
            settings.AlwaysRunAsAdministrator,
            settings.CheckForUpdatesAutomatically,
            settings.DetailedDiagnosticsEnabled,
            settings.Theme,
            settings.Language);

        public GeneralSettings ToSettings()
        {
            if (StartWithWindows is null ||
                CloseBehavior is null ||
                CursorLanding is null ||
                RecallOffscreenWindows is null ||
                ShowSuccessNotifications is null ||
                Theme is null ||
                Language is null ||
                !Enum.IsDefined(CloseBehavior.Value) ||
                !Enum.IsDefined(CursorLanding.Value) ||
                !Enum.IsDefined(Theme.Value) ||
                !Enum.IsDefined(Language.Value))
            {
                throw new JsonException("General settings contain missing or unsupported values.");
            }

            return new GeneralSettings(
                StartWithWindows.Value,
                CloseBehavior.Value,
                CursorLanding.Value,
                RecallOffscreenWindows.Value,
                ShowSuccessNotifications.Value,
                AlwaysRunAsAdministrator ?? false,
                CheckForUpdatesAutomatically ?? false,
                DetailedDiagnosticsEnabled ?? false,
                Theme.Value,
                Language.Value);
        }
    }
}
