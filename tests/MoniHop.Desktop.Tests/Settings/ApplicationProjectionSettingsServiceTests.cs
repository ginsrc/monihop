using MoniHop.Core.ApplicationProjection;
using MoniHop.Desktop.Settings;
using ProjectionApplicationIdentity = MoniHop.Core.ApplicationProjection.ApplicationIdentity;

namespace MoniHop.Desktop.Tests.Settings;

public sealed class ApplicationProjectionSettingsServiceTests
{
    private static readonly ProjectionApplicationIdentity Browser =
        new(ApplicationIdentityKind.ExecutablePath, @"C:\Apps\browser.exe");

    [Fact]
    public void UpdateGlobalSettings_PreservesRules()
    {
        var store = new MemoryStore(new ApplicationProjectionSettings(
            false,
            null,
            [Rule(Browser, "stable-a")]));
        var service = new ApplicationProjectionSettingsService(store);

        service.UpdateGlobalSettings(true, "stable-b");

        Assert.True(service.Current.IsEnabled);
        Assert.Equal("stable-b", service.Current.DefaultTargetDisplayId);
        Assert.Single(service.Current.Rules);
    }

    [Fact]
    public void SaveRule_ReplacesIdentityMatchWithoutCreatingDuplicate()
    {
        var store = new MemoryStore(ApplicationProjectionSettings.Default);
        var service = new ApplicationProjectionSettingsService(store);
        service.SaveRule(Rule(Browser, "stable-a"));

        service.SaveRule(Rule(
            new ProjectionApplicationIdentity(ApplicationIdentityKind.ExecutablePath, @"c:\apps\BROWSER.exe"),
            "stable-b"));

        Assert.Equal("stable-b", Assert.Single(service.Current.Rules).TargetDisplayId);
        Assert.Equal(2, store.SaveCount);
    }

    [Fact]
    public void DeleteRule_RemovesOnlyMatchingIdentity()
    {
        var editor = new ProjectionApplicationIdentity(ApplicationIdentityKind.ExecutablePath, @"C:\Apps\editor.exe");
        var store = new MemoryStore(new ApplicationProjectionSettings(
            true,
            null,
            [Rule(Browser, "stable-a"), Rule(editor, "stable-b")]));
        var service = new ApplicationProjectionSettingsService(store);

        service.DeleteRule(Browser);

        Assert.Equal(editor, Assert.Single(service.Current.Rules).Application);
    }

    private static ApplicationProjectionRule Rule(ProjectionApplicationIdentity identity, string target) =>
        new(identity, Path.GetFileNameWithoutExtension(identity.Value), target, ProjectionLayout.KeepSize, true);

    private sealed class MemoryStore(ApplicationProjectionSettings settings) : IApplicationProjectionStore
    {
        public int SaveCount { get; private set; }
        public ApplicationProjectionSettings Settings { get; private set; } = settings;
        public ApplicationProjectionSettings Load() => Settings;
        public void Save(ApplicationProjectionSettings value)
        {
            SaveCount++;
            Settings = value;
        }
    }
}
