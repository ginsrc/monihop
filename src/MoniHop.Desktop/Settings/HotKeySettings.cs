using MoniHop.Desktop.Models;
using MoniHop.Windows.HotKeys;

namespace MoniHop.Desktop.Settings;

public sealed class HotKeySettings : IEquatable<HotKeySettings>
{
    public HotKeySettings(IReadOnlyDictionary<string, HotKeyGesture>? bindings)
    {
        Bindings = new Dictionary<string, HotKeyGesture>(
            bindings ?? new Dictionary<string, HotKeyGesture>(),
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyDictionary<string, HotKeyGesture> Bindings { get; }

    public static HotKeySettings CreateDefaults() => new(
        HotKeyCatalog.All
            .Where(item => item.DefaultGesture is not null)
            .ToDictionary(item => item.Id, item => item.DefaultGesture!.Value, StringComparer.OrdinalIgnoreCase));

    public HotKeyGesture? Get(string actionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
        return Bindings.TryGetValue(actionId, out var gesture) ? gesture : null;
    }

    public HotKeyGesture? Get(HotKeyCommand command) => Get(HotKeyCatalog.Get(command).Id);

    public HotKeySettings With(string actionId, HotKeyGesture? gesture)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId);
        var bindings = new Dictionary<string, HotKeyGesture>(Bindings, StringComparer.OrdinalIgnoreCase);
        if (gesture is null)
        {
            bindings.Remove(actionId);
        }
        else
        {
            bindings[actionId] = gesture.Value;
        }

        return new HotKeySettings(bindings);
    }

    public HotKeySettings With(HotKeyCommand command, HotKeyGesture? gesture) =>
        With(HotKeyCatalog.Get(command).Id, gesture);

    public bool Equals(HotKeySettings? other) =>
        other is not null && Bindings.Count == other.Bindings.Count &&
        Bindings.All(item => other.Bindings.TryGetValue(item.Key, out var value) && value == item.Value);

    public override bool Equals(object? obj) => Equals(obj as HotKeySettings);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in Bindings.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
        {
            hash.Add(item.Key, StringComparer.OrdinalIgnoreCase);
            hash.Add(item.Value);
        }

        return hash.ToHashCode();
    }
}
