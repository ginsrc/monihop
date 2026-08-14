using MoniHop.Core.Displays;

namespace MoniHop.Desktop.Settings;

public sealed class DisplayProfileService
{
    private readonly IDisplayCatalog _displayCatalog;
    private readonly IDisplayProfileStore _store;
    private IReadOnlyList<DisplayProfileState> _states = [];

    public DisplayProfileService(IDisplayCatalog displayCatalog, IDisplayProfileStore store)
    {
        _displayCatalog = displayCatalog ?? throw new ArgumentNullException(nameof(displayCatalog));
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public IReadOnlyList<DisplayProfileState> States => _states;

    public event EventHandler? Changed;

    public DisplayProfileRefreshResult Refresh()
    {
        var profiles = _states.Count == 0
            ? _store.Load()
            : _states.Select(state => state.Profile).ToArray();
        var updated = DisplayProfileRegistry.Reconcile(
            profiles,
            _displayCatalog.ReadAll(),
            DateTimeOffset.UtcNow);

        _store.Save(updated.Select(state => state.Profile).ToArray());
        _states = updated;
        Changed?.Invoke(this, EventArgs.Empty);
        return new DisplayProfileRefreshResult(_states);
    }

    public void Rename(string stableId, string customName)
    {
        var renamed = DisplayProfileRegistry.Rename(
            _states.Select(state => state.Profile).ToArray(),
            stableId,
            customName);
        _store.Save(renamed);
        _states = DisplayProfileRegistry.Reconcile(
            renamed,
            _states.Where(state => state.CurrentDisplay is not null)
                .Select(state => state.CurrentDisplay!)
                .ToArray(),
            DateTimeOffset.UtcNow);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Forget(string stableId)
    {
        var state = _states.FirstOrDefault(item =>
            StringComparer.OrdinalIgnoreCase.Equals(item.Profile.StableId, stableId));
        if (state is null)
        {
            return;
        }

        if (state.IsConnected)
        {
            throw new InvalidOperationException("连接中的显示器不能忘记。");
        }

        var remaining = DisplayProfileRegistry.Forget(
            _states.Select(item => item.Profile).ToArray(),
            stableId);
        _store.Save(remaining);
        _states = _states
            .Where(item => !StringComparer.OrdinalIgnoreCase.Equals(item.Profile.StableId, stableId))
            .ToArray();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public IReadOnlyList<DisplaySnapshot> ApplyNames(IReadOnlyList<DisplaySnapshot> displays)
    {
        ArgumentNullException.ThrowIfNull(displays);
        var names = _states.ToDictionary(
            state => state.Profile.StableId,
            state => state.DisplayName,
            StringComparer.OrdinalIgnoreCase);
        return displays.Select(display =>
        {
            if (!names.TryGetValue(display.StableId, out var name) ||
                string.Equals(display.DisplayName, name, StringComparison.Ordinal))
            {
                return display;
            }

            return new DisplaySnapshot(
                display.DeviceName,
                name,
                display.Bounds,
                display.WorkingArea,
                display.IsPrimary,
                display.StableId,
                display.RefreshRateHz,
                display.ScalePercent,
                display.Orientation,
                display.PhysicalWidthMillimeters,
                display.PhysicalHeightMillimeters,
                display.ResolutionWidth,
                display.ResolutionHeight);
        }).ToArray();
    }
}

public sealed record DisplayProfileRefreshResult(IReadOnlyList<DisplayProfileState> States);
