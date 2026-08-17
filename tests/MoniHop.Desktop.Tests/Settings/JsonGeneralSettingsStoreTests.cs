using MoniHop.Desktop.Settings;
using MoniHop.Core.Cursors;

namespace MoniHop.Desktop.Tests.Settings;

public sealed class JsonGeneralSettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "MoniHop.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var store = new JsonGeneralSettingsStore(Path.Combine(_directory, "missing.json"));

        Assert.Equal(GeneralSettings.CreateDefaults(), store.Load());
    }

    [Fact]
    public void SaveAndLoad_RoundTripsEverySetting()
    {
        var path = Path.Combine(_directory, "general.json");
        var store = new JsonGeneralSettingsStore(path);
        var expected = new GeneralSettings(
            true,
            CloseBehavior.MinimizeToTray,
            CursorLandingMode.Center,
            true,
            false,
            true,
            true,
            true,
            AppTheme.Dark,
            AppLanguage.English);

        store.Save(expected);

        Assert.Equal(expected, store.Load());
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"Theme\":\"Neon\"}")]
    [InlineData("null")]
    public void Load_InvalidContent_ReturnsDefaults(string content)
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "general.json");
        File.WriteAllText(path, content);

        var settings = new JsonGeneralSettingsStore(path).Load();

        Assert.Equal(GeneralSettings.CreateDefaults(), settings);
        Assert.False(File.Exists(path));
        Assert.Single(Directory.GetFiles(_directory, "general.json.corrupt-*.json"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
