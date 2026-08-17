using MoniHop.Desktop.Settings;
using MoniHop.Core.Cursors;

namespace MoniHop.Desktop.Tests.Settings;

public sealed class GeneralSettingsServiceTests
{
    [Fact]
    public void Defaults_DoNotEnableAutomaticOrElevatedBehavior()
    {
        var settings = GeneralSettings.CreateDefaults();

        Assert.False(settings.StartWithWindows);
        Assert.Equal(CloseBehavior.Ask, settings.CloseBehavior);
        Assert.Equal(CursorLandingMode.Relative, settings.CursorLanding);
        Assert.False(settings.RecallOffscreenWindows);
        Assert.True(settings.ShowSuccessNotifications);
        Assert.False(settings.AlwaysRunAsAdministrator);
        Assert.False(settings.CheckForUpdatesAutomatically);
        Assert.False(settings.DetailedDiagnosticsEnabled);
        Assert.Equal(AppTheme.System, settings.Theme);
        Assert.Equal(AppLanguage.System, settings.Language);
    }

    [Fact]
    public void Update_PersistsBeforePublishingChanged()
    {
        var store = new MemoryGeneralSettingsStore(GeneralSettings.CreateDefaults());
        var service = new GeneralSettingsService(store);
        var observedSaveCount = -1;
        service.Changed += (_, _) => observedSaveCount = store.SaveCount;

        var updated = service.Update(service.Current with { Theme = AppTheme.Dark });

        Assert.True(updated);
        Assert.Equal(AppTheme.Dark, service.Current.Theme);
        Assert.Equal(1, store.SaveCount);
        Assert.Equal(1, observedSaveCount);
    }

    [Fact]
    public void Update_WhenSaveFails_KeepsPreviousValueAndDoesNotPublishChanged()
    {
        var store = new MemoryGeneralSettingsStore(GeneralSettings.CreateDefaults())
        {
            SaveException = new IOException("disk unavailable"),
        };
        var service = new GeneralSettingsService(store);
        var changeCount = 0;
        service.Changed += (_, _) => changeCount++;

        var updated = service.Update(service.Current with { Language = AppLanguage.English });

        Assert.False(updated);
        Assert.Equal(AppLanguage.System, service.Current.Language);
        Assert.Equal(0, changeCount);
        Assert.Equal("disk unavailable", service.LastError);
    }

    private sealed class MemoryGeneralSettingsStore(GeneralSettings settings) : IGeneralSettingsStore
    {
        public int SaveCount { get; private set; }

        public Exception? SaveException { get; init; }

        public GeneralSettings Load() => settings;

        public void Save(GeneralSettings value)
        {
            if (SaveException is not null)
            {
                throw SaveException;
            }

            settings = value;
            SaveCount++;
        }
    }
}
