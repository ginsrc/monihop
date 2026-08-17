using MoniHop.Core.Displays;
using MoniHop.Desktop.Settings;

namespace MoniHop.Desktop.Tests.Settings;

public sealed class JsonDisplayProfileStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"MoniHop.Tests.{Guid.NewGuid():N}");

    [Fact]
    public void SaveAndLoad_RoundTripsProfiles()
    {
        var path = Path.Combine(_directory, "display-profiles.json");
        var store = new JsonDisplayProfileStore(path);
        var profiles = new[]
        {
            new DisplayProfile(
                "stable-a",
                "Work screen",
                "Display 1",
                "1920 × 1080",
                false,
                new DateTimeOffset(2026, 8, 13, 10, 0, 0, TimeSpan.Zero)),
        };

        store.Save(profiles);
        var loaded = store.Load();

        Assert.Equal(profiles, loaded);
    }

    [Fact]
    public void Load_ReturnsEmptyWhenFileDoesNotExist()
    {
        var store = new JsonDisplayProfileStore(Path.Combine(_directory, "missing.json"));

        Assert.Empty(store.Load());
    }

    [Fact]
    public void Load_InvalidJson_QuarantinesOriginalAndReturnsEmpty()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "display-profiles.json");
        File.WriteAllText(path, "{broken");

        var loaded = new JsonDisplayProfileStore(path).Load();

        Assert.Empty(loaded);
        Assert.False(File.Exists(path));
        Assert.Single(Directory.GetFiles(_directory, "display-profiles.json.corrupt-*.json"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
