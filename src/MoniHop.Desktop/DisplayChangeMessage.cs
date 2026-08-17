namespace MoniHop.Desktop;

public static class DisplayChangeMessage
{
    private const int SettingChange = 0x001A;
    private const int DeviceModeChange = 0x001B;
    private const int DisplayChange = 0x007E;
    public static bool RequiresRefresh(int message) => message is
        DeviceModeChange or
        DisplayChange;

    public static bool RequiresThemeRefresh(int message) => message == SettingChange;
}
