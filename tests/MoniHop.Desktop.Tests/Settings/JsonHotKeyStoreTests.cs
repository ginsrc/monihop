using MoniHop.Desktop.Models;
using MoniHop.Desktop.Settings;
using MoniHop.Windows.HotKeys;

namespace MoniHop.Desktop.Tests.Settings;

public sealed class JsonHotKeyStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "MoniHop.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void SaveAndLoad_RoundTripsConfiguredAndUnsetBindings()
    {
        var path = Path.Combine(_directory, "hotkeys.json");
        var store = new JsonHotKeyStore(path);
        var settings = HotKeySettings.CreateDefaults().With(
            HotKeyCatalog.Get(HotKeyCommand.CursorNext).Id,
            new HotKeyGesture(HotKeyModifiers.Control | HotKeyModifiers.Alt, 0x50));

        store.Save(settings);
        var loaded = store.Load();

        Assert.Equal(settings.Bindings, loaded.Bindings);
        Assert.Null(loaded.Get(HotKeyCatalog.Get(HotKeyCommand.OpenProjectionPanel).Id));
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var settings = new JsonHotKeyStore(Path.Combine(_directory, "missing.json")).Load();

        Assert.Equal(HotKeySettings.CreateDefaults(), settings);
    }

    [Fact]
    public void Load_InvalidGesture_ReturnsDefaults()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "invalid.json");
        File.WriteAllText(path, """
            {
              "CursorNext": { "Modifiers": 0, "VirtualKey": 0 },
              "WindowPrevious": null,
              "WindowNext": null
            }
            """);

        var settings = new JsonHotKeyStore(path).Load();

        Assert.Equal(HotKeySettings.CreateDefaults(), settings);
    }

    [Fact]
    public void Load_LegacyThreePropertyFile_MigratesBindings()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "legacy.json");
        File.WriteAllText(path, """
            {
              "CursorNext": { "Modifiers": 3, "VirtualKey": 80 },
              "WindowPrevious": null,
              "WindowNext": { "Modifiers": 7, "VirtualKey": 39 }
            }
            """);

        var settings = new JsonHotKeyStore(path).Load();

        Assert.Equal(
            new HotKeyGesture(HotKeyModifiers.Control | HotKeyModifiers.Alt, 80),
            settings.Get(HotKeyCatalog.Get(HotKeyCommand.CursorNext).Id));
        Assert.Null(settings.Get(HotKeyCatalog.Get(HotKeyCommand.WindowPrevious).Id));
        Assert.Equal(
            new HotKeyGesture(HotKeyModifiers.Control | HotKeyModifiers.Alt | HotKeyModifiers.Shift, 39),
            settings.Get(HotKeyCatalog.Get(HotKeyCommand.WindowNext).Id));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
