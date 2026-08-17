using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace MoniHop.Windows.Security;

 [SupportedOSPlatform("windows")]
public sealed class NativeProcessElevationService : IProcessElevationService
{
    private readonly IProcessLauncher _launcher;
    private readonly Func<bool> _elevationProbe;

    public NativeProcessElevationService()
        : this(new NativeProcessLauncher(), IsCurrentProcessElevated)
    {
    }

    public NativeProcessElevationService(IProcessLauncher launcher, Func<bool> elevationProbe)
    {
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        _elevationProbe = elevationProbe ?? throw new ArgumentNullException(nameof(elevationProbe));
    }

    public bool IsElevated => _elevationProbe();

    public ElevationRestartResult RestartElevated(
        string executablePath,
        IReadOnlyList<string> arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentNullException.ThrowIfNull(arguments);
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = true,
            Verb = "runas",
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        try
        {
            _launcher.Start(startInfo);
            return ElevationRestartResult.Started;
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == 1223)
        {
            return ElevationRestartResult.Canceled;
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            return ElevationRestartResult.Failed;
        }
    }

    private static bool IsCurrentProcessElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    private sealed class NativeProcessLauncher : IProcessLauncher
    {
        public void Start(ProcessStartInfo startInfo)
        {
            _ = Process.Start(startInfo) ?? throw new InvalidOperationException("Process did not start.");
        }
    }
}
