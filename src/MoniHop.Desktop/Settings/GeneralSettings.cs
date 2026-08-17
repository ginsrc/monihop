using MoniHop.Core.Cursors;

namespace MoniHop.Desktop.Settings;

public enum CloseBehavior
{
    Ask,
    MinimizeToTray,
    Exit,
}

public enum AppTheme
{
    System,
    Light,
    Dark,
}

public enum AppLanguage
{
    System,
    SimplifiedChinese,
    English,
}

public sealed record GeneralSettings(
    bool StartWithWindows,
    CloseBehavior CloseBehavior,
    CursorLandingMode CursorLanding,
    bool RecallOffscreenWindows,
    bool ShowSuccessNotifications,
    bool AlwaysRunAsAdministrator,
    bool CheckForUpdatesAutomatically,
    bool DetailedDiagnosticsEnabled,
    AppTheme Theme,
    AppLanguage Language)
{
    public static GeneralSettings CreateDefaults() => new(
        StartWithWindows: false,
        CloseBehavior.Ask,
        CursorLandingMode.Relative,
        RecallOffscreenWindows: false,
        ShowSuccessNotifications: true,
        AlwaysRunAsAdministrator: false,
        CheckForUpdatesAutomatically: false,
        DetailedDiagnosticsEnabled: false,
        AppTheme.System,
        AppLanguage.System);
}
