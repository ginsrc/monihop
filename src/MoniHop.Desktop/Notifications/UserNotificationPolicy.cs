namespace MoniHop.Desktop.Notifications;

public static class UserNotificationPolicy
{
    public static bool ShouldShow(bool showSuccessNotifications, UserNotificationSeverity severity) =>
        severity != UserNotificationSeverity.Success || showSuccessNotifications;
}

public enum UserNotificationSeverity
{
    Success,
    Warning,
    Error,
}
