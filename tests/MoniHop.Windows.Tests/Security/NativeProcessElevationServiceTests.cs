using System.ComponentModel;
using System.Runtime.Versioning;
using MoniHop.Windows.Security;

namespace MoniHop.Windows.Tests.Security;

[SupportedOSPlatform("windows")]
public sealed class NativeProcessElevationServiceTests
{
    [Fact]
    public void RestartElevated_UsesRunAsAndPreservesArguments()
    {
        var launcher = new RecordingProcessLauncher();
        var service = new NativeProcessElevationService(launcher, () => false);

        var result = service.RestartElevated("C:\\Apps\\MoniHop.exe", ["--background", "two words"]);

        Assert.Equal(ElevationRestartResult.Started, result);
        Assert.Equal("runas", launcher.StartInfo!.Verb);
        Assert.True(launcher.StartInfo.UseShellExecute);
        Assert.Equal("C:\\Apps\\MoniHop.exe", launcher.StartInfo.FileName);
        Assert.Equal(["--background", "two words"], launcher.StartInfo.ArgumentList);
    }

    [Fact]
    public void RestartElevated_WhenUserCancelsUac_ReturnsCanceled()
    {
        var launcher = new RecordingProcessLauncher
        {
            Exception = new Win32Exception(1223),
        };
        var service = new NativeProcessElevationService(launcher, () => false);

        var result = service.RestartElevated("MoniHop.exe", []);

        Assert.Equal(ElevationRestartResult.Canceled, result);
    }

    [Fact]
    public void IsElevated_UsesInjectedWindowsIdentityProbe()
    {
        var service = new NativeProcessElevationService(new RecordingProcessLauncher(), () => true);

        Assert.True(service.IsElevated);
    }

    private sealed class RecordingProcessLauncher : IProcessLauncher
    {
        public System.Diagnostics.ProcessStartInfo? StartInfo { get; private set; }

        public Exception? Exception { get; init; }

        public void Start(System.Diagnostics.ProcessStartInfo startInfo)
        {
            StartInfo = startInfo;
            if (Exception is not null)
            {
                throw Exception;
            }
        }
    }
}
