namespace MoniHop.Desktop.Settings;

public interface IGeneralSettingsStore
{
    GeneralSettings Load();

    void Save(GeneralSettings settings);
}
