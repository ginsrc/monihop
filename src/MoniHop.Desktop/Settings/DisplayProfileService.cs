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
    }
}

public sealed record DisplayProfileRefreshResult(IReadOnlyList<DisplayProfileState> States);
