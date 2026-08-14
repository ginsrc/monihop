namespace MoniHop.Desktop.Settings;

public interface IHotKeyStore
{
    HotKeySettings Load();

    void Save(HotKeySettings settings);
}
