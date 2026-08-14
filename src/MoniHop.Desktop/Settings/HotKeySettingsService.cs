using MoniHop.Desktop.Models;
using MoniHop.Windows.HotKeys;

namespace MoniHop.Desktop.Settings;

public sealed class HotKeySettingsService
{
    private readonly IHotKeyStore _store;

    public HotKeySettingsService(IHotKeyStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        Current = _store.Load();
    }

    public event EventHandler? Changed;

    public HotKeySettings Current { get; private set; }

    public bool TryUpdate(HotKeyCommand command, HotKeyGesture? gesture, out string? error)
    {
        var definition = HotKeyCatalog.Get(command);
        return TryUpdate(definition, gesture, HotKeyCatalog.All, out error);
    }

    public bool TryUpdate(
        HotKeyActionDefinition definition,
        HotKeyGesture? gesture,
        IReadOnlyList<HotKeyActionDefinition> definitions,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(definitions);

        if (gesture is not null)
        {
            var duplicateBinding = Current.Bindings.FirstOrDefault(item =>
                !StringComparer.OrdinalIgnoreCase.Equals(item.Key, definition.Id) &&
                item.Value == gesture);
            if (duplicateBinding.Key is not null)
            {
                var duplicate = definitions.FirstOrDefault(item =>
                    StringComparer.OrdinalIgnoreCase.Equals(item.Id, duplicateBinding.Key));
                error = duplicate is null
                    ? "该快捷键已用于已保留的显示器动作"
                    : $"该快捷键已用于“{duplicate.ActionName}”";
                return false;
            }
        }

        Save(Current.With(definition.Id, gesture));
        error = null;
        return true;
    }

    public void ResetDefaults() => Save(HotKeySettings.CreateDefaults());

    private void Save(HotKeySettings settings)
    {
        _store.Save(settings);
        Current = settings;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
