using MoniHop.Desktop.Models;
using MoniHop.Desktop.Settings;
using MoniHop.Windows.HotKeys;

namespace MoniHop.Desktop.Tests.Settings;

public sealed class HotKeySettingsServiceTests
{
    [Fact]
    public void Defaults_AssignOnlyTheThreeHighFrequencyActions()
    {
        var settings = HotKeySettings.CreateDefaults();

        Assert.Equal(
            new HotKeyGesture(HotKeyModifiers.Control | HotKeyModifiers.Alt, 0x4D),
            settings.Get(HotKeyCatalog.Get(HotKeyCommand.CursorNext).Id));
        Assert.Equal(
            new HotKeyGesture(HotKeyModifiers.Control | HotKeyModifiers.Alt | HotKeyModifiers.Shift, 0x25),
            settings.Get(HotKeyCatalog.Get(HotKeyCommand.WindowPrevious).Id));
        Assert.Equal(
            new HotKeyGesture(HotKeyModifiers.Control | HotKeyModifiers.Alt | HotKeyModifiers.Shift, 0x27),
            settings.Get(HotKeyCatalog.Get(HotKeyCommand.WindowNext).Id));

        Assert.All(
            HotKeyCatalog.All.Where(item => item.DefaultGesture is null),
            item => Assert.Null(settings.Get(item.Id)));
    }

    [Fact]
    public void Catalog_ListsEveryPlannedActionOnceAcrossTheFourGroups()
    {
        var actions = HotKeyCatalog.All;

        Assert.Equal(actions.Count, actions.Select(item => item.Id).Distinct().Count());
        Assert.Equal(
            ["鼠标", "窗口", "快捷投放", "恢复与程序"],
            actions.Select(item => item.Category).Distinct());
        Assert.All(actions, item => Assert.True(item.IsImplemented));
        Assert.Contains(actions, item => item.Command == HotKeyCommand.CursorNext);
        Assert.Contains(actions, item => item.Command == HotKeyCommand.OpenProjectionPanel);
        Assert.Contains(actions, item => item.Command == HotKeyCommand.RecallOffscreenWindows);
        Assert.Contains(actions, item => item.Command == HotKeyCommand.ToggleApplicationProjection);
    }

    [Fact]
    public void CreateForDisplays_AddsStableActionForEveryKnownDisplay()
    {
        var definitions = HotKeyCatalog.CreateForDisplays(
        [
            new HotKeyDisplayTarget("stable-a", "工作屏", true),
            new HotKeyDisplayTarget("stable-b", "便携屏", false),
        ]);

        var displayActions = definitions
            .Where(item => item.Command == HotKeyCommand.ProjectWindowToSpecificDisplay)
            .ToArray();

        Assert.Equal(2, displayActions.Length);
        Assert.Equal("投放到工作屏", displayActions[0].ActionName);
        Assert.Equal("stable-a", displayActions[0].TargetDisplayId);
        Assert.True(displayActions[0].IsTargetAvailable);
        Assert.Equal("stable-b", displayActions[1].TargetDisplayId);
        Assert.False(displayActions[1].IsTargetAvailable);
        Assert.NotEqual(displayActions[0].Id, displayActions[1].Id);
    }

    [Fact]
    public void TryUpdate_RejectsShortcutAlreadyUsedByAnotherAction()
    {
        var store = new MemoryHotKeyStore(HotKeySettings.CreateDefaults());
        var service = new HotKeySettingsService(store);
        var duplicate = service.Current.Get(HotKeyCatalog.Get(HotKeyCommand.CursorNext).Id);

        var changed = service.TryUpdate(HotKeyCommand.WindowNext, duplicate, out var error);

        Assert.False(changed);
        Assert.Equal("该快捷键已用于“鼠标切到下一屏”", error);
        Assert.Equal(HotKeySettings.CreateDefaults(), service.Current);
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public void TryUpdate_PersistsAnImplementedActionAndAllowsClearingIt()
    {
        var store = new MemoryHotKeyStore(HotKeySettings.CreateDefaults());
        var service = new HotKeySettingsService(store);
        var replacement = new HotKeyGesture(HotKeyModifiers.Control | HotKeyModifiers.Alt, 0x50);

        Assert.True(service.TryUpdate(HotKeyCommand.CursorNext, replacement, out var updateError));
        Assert.Null(updateError);
        Assert.Equal(replacement, service.Current.Get(HotKeyCatalog.Get(HotKeyCommand.CursorNext).Id));

        Assert.True(service.TryUpdate(HotKeyCommand.CursorNext, null, out var clearError));
        Assert.Null(clearError);
        Assert.Null(service.Current.Get(HotKeyCatalog.Get(HotKeyCommand.CursorNext).Id));
        Assert.Equal(2, store.SaveCount);
    }

    [Fact]
    public void TryUpdate_AllowsEveryCatalogAction()
    {
        var service = new HotKeySettingsService(new MemoryHotKeyStore(HotKeySettings.CreateDefaults()));

        var changed = service.TryUpdate(
            HotKeyCommand.OpenProjectionPanel,
            new HotKeyGesture(HotKeyModifiers.Control | HotKeyModifiers.Alt, 0x50),
            out var error);

        Assert.True(changed);
        Assert.Null(error);
        Assert.Equal(
            new HotKeyGesture(HotKeyModifiers.Control | HotKeyModifiers.Alt, 0x50),
            service.Current.Get(HotKeyCatalog.Get(HotKeyCommand.OpenProjectionPanel).Id));
    }

    [Fact]
    public void TryUpdate_PersistsDisplaySpecificBindingByStableActionId()
    {
        var service = new HotKeySettingsService(new MemoryHotKeyStore(HotKeySettings.CreateDefaults()));
        var definition = HotKeyCatalog.CreateForDisplays(
            [new HotKeyDisplayTarget("display\\stable", "副屏", true)])
            .Single(item => item.Command == HotKeyCommand.ProjectWindowToSpecificDisplay);
        var gesture = new HotKeyGesture(HotKeyModifiers.Control | HotKeyModifiers.Alt, 0x32);

        var changed = service.TryUpdate(definition, gesture, HotKeyCatalog.CreateForDisplays(
            [new HotKeyDisplayTarget("display\\stable", "副屏", true)]), out var error);

        Assert.True(changed);
        Assert.Null(error);
        Assert.Equal(gesture, service.Current.Get(definition.Id));
    }

    [Fact]
    public void TryUpdate_RejectsGestureReservedByTemporarilyHiddenDisplayBinding()
    {
        var reserved = new HotKeyGesture(HotKeyModifiers.Control | HotKeyModifiers.Alt, 0x32);
        var settings = HotKeySettings.CreateDefaults().With("projection.display:disconnected", reserved);
        var service = new HotKeySettingsService(new MemoryHotKeyStore(settings));
        var definition = HotKeyCatalog.Get(HotKeyCommand.OpenProjectionPanel);

        var changed = service.TryUpdate(definition, reserved, HotKeyCatalog.All, out var error);

        Assert.False(changed);
        Assert.Equal("该快捷键已用于已保留的显示器动作", error);
    }

    private sealed class MemoryHotKeyStore(HotKeySettings settings) : IHotKeyStore
    {
        public int SaveCount { get; private set; }

        public HotKeySettings Load() => settings;

        public void Save(HotKeySettings value)
        {
            settings = value;
            SaveCount++;
        }
    }
}
