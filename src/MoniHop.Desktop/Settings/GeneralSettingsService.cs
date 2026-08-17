using System.IO;

namespace MoniHop.Desktop.Settings;

public sealed class GeneralSettingsService
{
    private readonly IGeneralSettingsStore _store;

    public GeneralSettingsService(IGeneralSettingsStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        Current = _store.Load();
    }

    public event EventHandler? Changed;

    public GeneralSettings Current { get; private set; }

    public string? LastError { get; private set; }

    public bool Update(GeneralSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings == Current)
        {
            LastError = null;
            return true;
        }

        try
        {
            _store.Save(settings);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            LastError = exception.Message;
            return false;
        }

        Current = settings;
        LastError = null;
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }
}
