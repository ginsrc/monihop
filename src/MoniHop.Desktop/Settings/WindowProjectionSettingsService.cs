using MoniHop.Core.ApplicationProjection;
using MoniHop.Core.WindowProjection;

namespace MoniHop.Desktop.Settings;

public sealed class WindowProjectionSettingsService
{
    private readonly IWindowProjectionStore _store;

    public WindowProjectionSettingsService(IWindowProjectionStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        Current = _store.Load();
    }

    public event EventHandler? Changed;

    public WindowProjectionSettings Current { get; private set; }

    public void UpdateEnabled(bool isEnabled) => Save(Create(isEnabled: isEnabled));

    public void UpdateTriggerMode(WindowProjectionTriggerMode triggerMode) => Save(Create(triggerMode: triggerMode));

    public void UpdatePosition(RelativePosition relativePosition) => Save(Create(relativePosition: relativePosition, isPositionLocked: true));

    public void UpdateDefaultTarget(string? targetDisplayId) => Save(new WindowProjectionSettings(
        Current.IsEnabled,
        Current.TriggerMode,
        Current.IsPositionLocked,
        Current.RelativePosition,
        targetDisplayId,
        Current.DefaultLayout));

    public void UpdateDefaultLayout(ProjectionLayout layout) => Save(Create(defaultLayout: layout));

    public void SetPositionEditing(bool isEditing) => Save(Create(isPositionLocked: !isEditing));

    public void ResetPosition() => Save(new WindowProjectionSettings(
        Current.IsEnabled,
        Current.TriggerMode,
        true,
        WindowProjectionSettings.Default.RelativePosition,
        Current.DefaultTargetDisplayId,
        Current.DefaultLayout));

    private WindowProjectionSettings Create(
        bool? isEnabled = null,
        WindowProjectionTriggerMode? triggerMode = null,
        bool? isPositionLocked = null,
        RelativePosition? relativePosition = null,
        string? defaultTargetDisplayId = null,
        ProjectionLayout? defaultLayout = null) =>
        new(
            isEnabled ?? Current.IsEnabled,
            triggerMode ?? Current.TriggerMode,
            isPositionLocked ?? Current.IsPositionLocked,
            relativePosition ?? Current.RelativePosition,
            defaultTargetDisplayId ?? Current.DefaultTargetDisplayId,
            defaultLayout ?? Current.DefaultLayout);

    private void Save(WindowProjectionSettings settings)
    {
        _store.Save(settings);
        Current = settings;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
