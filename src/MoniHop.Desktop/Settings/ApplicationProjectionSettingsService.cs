using MoniHop.Core.ApplicationProjection;
using ProjectionApplicationIdentity = MoniHop.Core.ApplicationProjection.ApplicationIdentity;

namespace MoniHop.Desktop.Settings;

public sealed class ApplicationProjectionSettingsService
{
    private readonly IApplicationProjectionStore _store;

    public ApplicationProjectionSettingsService(IApplicationProjectionStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        Current = _store.Load();
    }

    public event EventHandler? Changed;

    public ApplicationProjectionSettings Current { get; private set; }

    public void UpdateGlobalSettings(bool isEnabled, string? defaultTargetDisplayId) =>
        Save(new ApplicationProjectionSettings(
            isEnabled,
            defaultTargetDisplayId,
            Current.Rules));

    public void SaveRule(ApplicationProjectionRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        var rules = Current.Rules
            .Where(item => !item.Application.Matches(rule.Application))
            .Append(rule)
            .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        Save(new ApplicationProjectionSettings(
            Current.IsEnabled,
            Current.DefaultTargetDisplayId,
            rules));
    }

    public void DeleteRule(ProjectionApplicationIdentity application)
    {
        ArgumentNullException.ThrowIfNull(application);
        Save(new ApplicationProjectionSettings(
            Current.IsEnabled,
            Current.DefaultTargetDisplayId,
            Current.Rules.Where(item => !item.Application.Matches(application)).ToArray()));
    }

    private void Save(ApplicationProjectionSettings settings)
    {
        _store.Save(settings);
        Current = settings;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
