using MoniHop.Core.WindowProjection;
using MoniHop.Desktop.Settings;

namespace MoniHop.Desktop.Tests.Settings;

public sealed class JsonWindowProjectionStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"MoniHop.WindowProjection.{Guid.NewGuid():N}");

    [Fact]
    public void SaveAndLoad_RoundTripsSettings()
    {
        var store = new JsonWindowProjectionStore(Path.Combine(_directory, "window-projection.json"));
        var settings = new WindowProjectionSettings(
            true,
            WindowProjectionTriggerMode.Immediate,
            false,
            new RelativePosition(.4, .6),
            "stable-b",
            MoniHop.Core.ApplicationProjection.ProjectionLayout.RightHalf);

        store.Save(settings);

        var loaded = store.Load();
        Assert.Equal(settings, loaded);
    }

    [Fact]
    public void Load_ReturnsDefaultForInvalidJson()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "window-projection.json"), "{broken");

        var settings = new JsonWindowProjectionStore(Path.Combine(_directory, "window-projection.json")).Load();

        Assert.Equal(WindowProjectionSettings.Default, settings);
    }

    [Fact]
    public void Load_ReturnsDefaultForStructurallyInvalidSettings()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "window-projection.json");
        File.WriteAllText(path, """
            {
              "IsEnabled": true,
              "TriggerMode": 99,
              "IsPositionLocked": true,
              "RelativePosition": { "X": 0.5, "Y": 0.1 }
            }
            """);

        var settings = new JsonWindowProjectionStore(path).Load();

        Assert.Equal(WindowProjectionSettings.Default, settings);
    }

    [Fact]
    public void Load_OldSettingsWithoutDefaultDropFieldsUsesSafeDefaults()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "window-projection.json");
        File.WriteAllText(path, """
            {
              "IsEnabled": true,
              "TriggerMode": 1,
              "IsPositionLocked": true,
              "RelativePosition": { "X": 0.5, "Y": 0.1 }
            }
            """);

        var settings = new JsonWindowProjectionStore(path).Load();

        Assert.True(settings.IsEnabled);
        Assert.Null(settings.DefaultTargetDisplayId);
        Assert.Equal(
            MoniHop.Core.ApplicationProjection.ProjectionLayout.Maximized,
            settings.DefaultLayout);
    }

    [Fact]
    public void Load_MigratesPreviousDefaultPortalPositionToTopCenter()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "window-projection.json");
        File.WriteAllText(path, """
            {
              "IsEnabled": true,
              "TriggerMode": 0,
              "IsPositionLocked": true,
              "RelativePosition": { "X": 0.72, "Y": 0.08 },
              "DefaultTargetDisplayId": null,
              "DefaultLayout": 1
            }
            """);

        var settings = new JsonWindowProjectionStore(path).Load();

        Assert.Equal(new RelativePosition(.5, 0), settings.RelativePosition);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }
}
