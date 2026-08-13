using MoniHop.Core.Displays;
using MoniHop.Desktop.Settings;

namespace MoniHop.Desktop.Tests.Settings;

public sealed class DisplayProfileServiceTests
{
    [Fact]
    public void Refresh_AfterDisconnectUpdatesConnectedSetAndRetainsHistory()
    {
        var catalog = new MutableDisplayCatalog(
            Display("DISPLAY1", "stable-a"),
            Display("DISPLAY2", "stable-b"));
        var store = new MemoryDisplayProfileStore();
        var service = new DisplayProfileService(catalog, store);

        service.Refresh();
        catalog.Displays = [Display("DISPLAY1", "stable-a")];
        var result = service.Refresh();

        Assert.Single(result.States, state => state.IsConnected);
        Assert.Contains(result.States, state =>
            state.Profile.StableId == "stable-b" && !state.IsConnected);
        Assert.Equal(2, store.Profiles.Count);
    }

    [Fact]
    public void Rename_SavesBeforePublishingNewName()
    {
        var catalog = new MutableDisplayCatalog(Display("DISPLAY1", "stable-a"));
        var store = new MemoryDisplayProfileStore();
        var service = new DisplayProfileService(catalog, store);
        service.Refresh();

        service.Rename("stable-a", "Work screen");

        Assert.Equal("Work screen", Assert.Single(service.States).DisplayName);
        Assert.Equal("Work screen", Assert.Single(store.Profiles).CustomName);
    }

    [Fact]
    public void Forget_RejectsConnectedDisplayAndRemovesDisconnectedDisplay()
    {
        var catalog = new MutableDisplayCatalog(Display("DISPLAY1", "stable-a"));
        var store = new MemoryDisplayProfileStore();
        var service = new DisplayProfileService(catalog, store);
        service.Refresh();

        Assert.Throws<InvalidOperationException>(() => service.Forget("stable-a"));

        catalog.Displays = [];
        service.Refresh();
        service.Forget("stable-a");

        Assert.Empty(service.States);
        Assert.Empty(store.Profiles);
    }

    private static DisplaySnapshot Display(string deviceName, string stableId) =>
        new(
            deviceName,
            deviceName,
            new PixelRect(0, 0, 1920, 1080),
            new PixelRect(0, 0, 1920, 1040),
            deviceName.EndsWith('1'),
            stableId);

    private sealed class MutableDisplayCatalog(params DisplaySnapshot[] displays) : IDisplayCatalog
    {
        public IReadOnlyList<DisplaySnapshot> Displays { get; set; } = displays;

        public IReadOnlyList<DisplaySnapshot> ReadAll() => Displays;
    }

    private sealed class MemoryDisplayProfileStore : IDisplayProfileStore
    {
        public string FilePath => "memory://display-profiles";

        public IReadOnlyList<DisplayProfile> Profiles { get; private set; } = [];

        public IReadOnlyList<DisplayProfile> Load() => Profiles;

        public void Save(IReadOnlyList<DisplayProfile> profiles) =>
            Profiles = profiles.ToArray();
    }
}
