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
    public void Rename_PublishesChangeAndAppliesCustomNameToLiveSnapshots()
    {
        var display = Display("DISPLAY1", "stable-a");
        var service = new DisplayProfileService(
            new MutableDisplayCatalog(display),
            new MemoryDisplayProfileStore());
        service.Refresh();
        var changedCount = 0;
        service.Changed += (_, _) => changedCount++;

        service.Rename("stable-a", "Work screen");
        var named = service.ApplyNames([display]);

        Assert.Equal(1, changedCount);
        Assert.Equal("Work screen", Assert.Single(named).DisplayName);
    }

    [Fact]
    public void Refresh_WhenDisplayTopologyHasNotChanged_DoesNotSaveOrPublishAgain()
    {
        var catalog = new MutableDisplayCatalog(Display("DISPLAY1", "stable-a"));
        var store = new MemoryDisplayProfileStore();
        var service = new DisplayProfileService(catalog, store);
        service.Refresh();
        var changedCount = 0;
        service.Changed += (_, _) => changedCount++;

        service.Refresh();

        Assert.Equal(1, store.SaveCount);
        Assert.Equal(0, changedCount);
    }

    [Fact]
    public void Rename_PublishesProfileChangeWithoutTopologyChange()
    {
        var service = new DisplayProfileService(
            new MutableDisplayCatalog(Display("DISPLAY1", "stable-a")),
            new MemoryDisplayProfileStore());
        service.Refresh();
        var topologyChanged = 0;
        var profilesChanged = 0;
        service.TopologyChanged += (_, _) => topologyChanged++;
        service.ProfilesChanged += (_, _) => profilesChanged++;

        service.Rename("stable-a", "Work screen");

        Assert.Equal(0, topologyChanged);
        Assert.Equal(1, profilesChanged);
    }

    [Fact]
    public void Refresh_WhenOnlyDisplayMetadataChanges_DoesNotRequestWindowRecall()
    {
        var catalog = new MutableDisplayCatalog(Display("DISPLAY1", "stable-a"));
        var service = new DisplayProfileService(catalog, new MemoryDisplayProfileStore());
        service.Refresh();
        var topologyChanged = 0;
        service.TopologyChanged += (_, _) => topologyChanged++;
        catalog.Displays = [Display("DISPLAY1", "stable-a", refreshRateHz: 144)];

        service.Refresh();

        Assert.Equal(0, topologyChanged);
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

    private static DisplaySnapshot Display(string deviceName, string stableId, int? refreshRateHz = null) =>
        new(
            deviceName,
            deviceName,
            new PixelRect(0, 0, 1920, 1080),
            new PixelRect(0, 0, 1920, 1040),
            deviceName.EndsWith('1'),
            stableId,
            refreshRateHz);

    private sealed class MutableDisplayCatalog(params DisplaySnapshot[] displays) : IDisplayCatalog
    {
        public IReadOnlyList<DisplaySnapshot> Displays { get; set; } = displays;

        public IReadOnlyList<DisplaySnapshot> ReadAll() => Displays;
    }

    private sealed class MemoryDisplayProfileStore : IDisplayProfileStore
    {
        public string FilePath => "memory://display-profiles";

        public IReadOnlyList<DisplayProfile> Profiles { get; private set; } = [];

        public int SaveCount { get; private set; }

        public IReadOnlyList<DisplayProfile> Load() => Profiles;

        public void Save(IReadOnlyList<DisplayProfile> profiles)
        {
            SaveCount++;
            Profiles = profiles.ToArray();
        }
    }
}
