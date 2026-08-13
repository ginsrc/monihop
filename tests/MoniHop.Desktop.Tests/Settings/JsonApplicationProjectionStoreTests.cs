using MoniHop.Core.ApplicationProjection;
using MoniHop.Desktop.Settings;
using ProjectionApplicationIdentity = MoniHop.Core.ApplicationProjection.ApplicationIdentity;

namespace MoniHop.Desktop.Tests.Settings;

public sealed class JsonApplicationProjectionStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"MoniHop.Tests.{Guid.NewGuid():N}");

    [Fact]
    public void Load_ReturnsDefaultWhenFileDoesNotExist()
    {
        var store = new JsonApplicationProjectionStore(Path.Combine(_directory, "missing.json"));

        Assert.Equal(ApplicationProjectionSettings.Default, store.Load());
    }

    [Fact]
    public void SaveAndLoad_RoundTripsSettings()
    {
        var path = Path.Combine(_directory, "application-projection.json");
        var store = new JsonApplicationProjectionStore(path);
        var settings = new ApplicationProjectionSettings(
            true,
            "stable-b",
            [new ApplicationProjectionRule(
                new ProjectionApplicationIdentity(ApplicationIdentityKind.ExecutablePath, @"C:\Apps\browser.exe"),
                "Browser",
                "stable-a",
                ProjectionLayout.Maximized,
                true)]);

        store.Save(settings);
        var loaded = store.Load();

        Assert.Equal(settings.IsEnabled, loaded.IsEnabled);
        Assert.Equal(settings.DefaultTargetDisplayId, loaded.DefaultTargetDisplayId);
        Assert.Equal(Assert.Single(settings.Rules), Assert.Single(loaded.Rules));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
