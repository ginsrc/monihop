using System.Runtime.Versioning;
using MoniHop.Windows.Startup;

namespace MoniHop.Windows.Tests.Startup;

[SupportedOSPlatform("windows")]
public sealed class NativeStartupRegistrationServiceTests
{
    [Fact]
    public void BuildRunCommand_QuotesExecutableAndStartsInBackground()
    {
        Assert.Equal(
            "\"C:\\Program Files\\MoniHop\\MoniHop.exe\" --background",
            NativeStartupRegistrationService.BuildRunCommand(
                "C:\\Program Files\\MoniHop\\MoniHop.exe"));
    }

    [Fact]
    public void BuildElevatedTaskArguments_CreatesCurrentUserLogonTaskAtHighestLevel()
    {
        var arguments = NativeStartupRegistrationService.BuildElevatedTaskArguments(
            "C:\\Program Files\\MoniHop\\MoniHop.exe");

        Assert.Contains("/Create", arguments);
        Assert.Contains("/SC ONLOGON", arguments);
        Assert.Contains("/RL HIGHEST", arguments);
        Assert.Contains("/TN \"MoniHop Startup\"", arguments);
        Assert.Contains("--background", arguments);
    }
}
