using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace MoniHop.Windows.Startup;

[SupportedOSPlatform("windows")]
public sealed class NativeStartupRegistrationService(string executablePath) : IStartupRegistrationService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "MoniHop";
    private const string TaskName = "MoniHop Startup";

    public void Apply(bool enabled, bool runElevated)
    {
        if (!enabled)
        {
            RemoveRunValue();
            DeleteElevatedTask(ignoreMissing: true);
            return;
        }

        if (runElevated)
        {
            RemoveRunValue();
            RunSchtasks(BuildElevatedTaskArguments(executablePath), ignoreMissing: false);
            return;
        }

        DeleteElevatedTask(ignoreMissing: true);
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Windows startup key is unavailable.");
        key.SetValue(ValueName, BuildRunCommand(executablePath), RegistryValueKind.String);
    }

    public static string BuildRunCommand(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return $"\"{path}\" --background";
    }

    public static string BuildElevatedTaskArguments(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return $"/Create /F /SC ONLOGON /RL HIGHEST /TN \"{TaskName}\" /TR \"\\\"{path}\\\" --background\"";
    }

    private static void RemoveRunValue()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    private static void DeleteElevatedTask(bool ignoreMissing) =>
        RunSchtasks($"/Delete /F /TN \"{TaskName}\"", ignoreMissing);

    private static void RunSchtasks(string arguments, bool ignoreMissing)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "schtasks.exe"),
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        }) ?? throw new InvalidOperationException("Task Scheduler command did not start.");
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd().Trim();
            if (ignoreMissing && IsTaskNotFound(error))
            {
                return;
            }

            throw new Win32Exception(process.ExitCode, string.IsNullOrWhiteSpace(error)
                ? "Task Scheduler rejected the startup task."
                : error);
        }
    }

    private static bool IsTaskNotFound(string error) =>
        error.Contains("does not exist", StringComparison.OrdinalIgnoreCase) ||
        error.Contains("找不到", StringComparison.OrdinalIgnoreCase) ||
        error.Contains("不存在", StringComparison.OrdinalIgnoreCase);
}
