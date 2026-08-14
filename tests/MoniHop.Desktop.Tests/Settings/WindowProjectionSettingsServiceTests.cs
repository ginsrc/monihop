using MoniHop.Core.WindowProjection;
using MoniHop.Desktop.Settings;
using MoniHop.Core.ApplicationProjection;

namespace MoniHop.Desktop.Tests.Settings;

public sealed class WindowProjectionSettingsServiceTests
{
    [Fact]
    public void UpdateMethodsPersistOnlyRequestedFields()
    {
        var store = new MemoryStore(WindowProjectionSettings.Default);
        var service = new WindowProjectionSettingsService(store);

        service.UpdateEnabled(true);
        service.UpdateTriggerMode(WindowProjectionTriggerMode.Immediate);
        service.UpdatePosition(new RelativePosition(.2, .3));
        service.UpdateDefaultTarget("stable-b");
        service.UpdateDefaultLayout(MoniHop.Core.ApplicationProjection.ProjectionLayout.RightHalf);

        Assert.True(service.Current.IsEnabled);
        Assert.Equal(WindowProjectionTriggerMode.Immediate, service.Current.TriggerMode);
        Assert.Equal(new RelativePosition(.2, .3), service.Current.RelativePosition);
        Assert.True(service.Current.IsPositionLocked);
        Assert.Equal("stable-b", service.Current.DefaultTargetDisplayId);
        Assert.Equal(MoniHop.Core.ApplicationProjection.ProjectionLayout.RightHalf, service.Current.DefaultLayout);
        Assert.Equal(5, store.SaveCount);

        service.UpdateDefaultTarget(null);

        Assert.Null(service.Current.DefaultTargetDisplayId);
    }

    [Fact]
    public void ResetPositionKeepsActivationAndTriggerMode()
    {
        var store = new MemoryStore(new WindowProjectionSettings(
            true,
            WindowProjectionTriggerMode.Immediate,
            false,
            new RelativePosition(.1, .2),
            "stable-b",
            MoniHop.Core.ApplicationProjection.ProjectionLayout.LeftHalf));
        var service = new WindowProjectionSettingsService(store);

        service.ResetPosition();

        Assert.True(service.Current.IsEnabled);
        Assert.Equal(WindowProjectionTriggerMode.Immediate, service.Current.TriggerMode);
        Assert.True(service.Current.IsPositionLocked);
        Assert.Equal(WindowProjectionSettings.Default.RelativePosition, service.Current.RelativePosition);
        Assert.Equal("stable-b", service.Current.DefaultTargetDisplayId);
        Assert.Equal(MoniHop.Core.ApplicationProjection.ProjectionLayout.LeftHalf, service.Current.DefaultLayout);
    }

    [Fact]
    public void UpdateDefaultLayout_PersistsKeepSizeWithoutChangingOtherSettings()
    {
        var original = new WindowProjectionSettings(
            true,
            WindowProjectionTriggerMode.Immediate,
            true,
            new RelativePosition(.4, 0),
            "stable-b",
            ProjectionLayout.Maximized);
        var store = new MemoryStore(original);
        var service = new WindowProjectionSettingsService(store);

        service.UpdateDefaultLayout(ProjectionLayout.KeepSize);

        Assert.True(service.Current.IsEnabled);
        Assert.Equal(WindowProjectionTriggerMode.Immediate, service.Current.TriggerMode);
        Assert.Equal(new RelativePosition(.4, 0), service.Current.RelativePosition);
        Assert.Equal("stable-b", service.Current.DefaultTargetDisplayId);
        Assert.Equal(ProjectionLayout.KeepSize, service.Current.DefaultLayout);
        Assert.Equal(1, store.SaveCount);
    }

    private sealed class MemoryStore(WindowProjectionSettings settings) : IWindowProjectionStore
    {
        public int SaveCount { get; private set; }
        private WindowProjectionSettings Value { get; set; } = settings;
        public WindowProjectionSettings Load() => Value;
        public void Save(WindowProjectionSettings value) { SaveCount++; Value = value; }
    }
}
