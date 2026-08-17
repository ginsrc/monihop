namespace MoniHop.Desktop.Lifecycle;

public enum StartupLaunchAction
{
    Continue,
    RestartElevatedBeforeSingleInstance,
}

public static class StartupLaunchPolicy
{
    public static StartupLaunchAction Decide(bool alwaysRunAsAdministrator, bool isElevated) =>
        alwaysRunAsAdministrator && !isElevated
            ? StartupLaunchAction.RestartElevatedBeforeSingleInstance
            : StartupLaunchAction.Continue;
}
