namespace MoniHop.Desktop;

public static class DisplayChangeMessage
{
    private const int SettingChange = 0x001A;
    private const int DeviceModeChange = 0x001B;
    private const int DisplayChange = 0x007E;
    private const int DeviceChange = 0x0219;
    private const int DpiChanged = 0x02E0;

    public static bool RequiresRefresh(int message) => message is
        SettingChange or
        DeviceModeChange or
        DisplayChange or
        DeviceChange or
        DpiChanged;
}
