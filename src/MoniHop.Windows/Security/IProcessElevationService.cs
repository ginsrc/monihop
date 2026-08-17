using System.Diagnostics;

namespace MoniHop.Windows.Security;

public enum ElevationRestartResult
{
    Started,
    Canceled,
    Failed,
}
public interface IProcessLauncher
{
    void Start(ProcessStartInfo startInfo);
}

public interface IProcessElevationService
{
    bool IsElevated { get; }

    ElevationRestartResult RestartElevated(string executablePath, IReadOnlyList<string> arguments);
}
