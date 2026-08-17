using MoniHop.Desktop.Notifications;

namespace MoniHop.Desktop.Tests.Notifications;

public sealed class UserNotificationPolicyTests
{
    [Theory]
    [InlineData(UserNotificationSeverity.Warning)]
    [InlineData(UserNotificationSeverity.Error)]
    public void ShouldShow_WhenSuccessNotificationsAreDisabled_KeepsImportantFeedback(
        UserNotificationSeverity severity) =>
        Assert.True(UserNotificationPolicy.ShouldShow(false, severity));

    [Fact]
    public void ShouldShow_WhenSuccessNotificationsAreDisabled_HidesSuccess() =>
        Assert.False(UserNotificationPolicy.ShouldShow(false, UserNotificationSeverity.Success));
}
