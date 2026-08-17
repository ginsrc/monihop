using MoniHop.Desktop.Diagnostics;

namespace MoniHop.Desktop.Tests.Diagnostics;

public sealed class LocalDiagnosticServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        "MoniHop.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void Write_WhenDisabled_DoesNotCreateLog()
    {
        var path = Path.Combine(_directory, "monihop.log");
        var service = new LocalDiagnosticService(path, () => false);

        service.Write("startup", "ready");

        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Write_WhenForced_CapturesCriticalFailureEvenWhenDetailedDiagnosticsAreDisabled()
    {
        var path = Path.Combine(_directory, "monihop.log");
        var service = new LocalDiagnosticService(path, () => false);

        service.Write("startup.failed", "IOException", always: true);

        Assert.Contains("startup.failed", File.ReadAllText(path), StringComparison.Ordinal);
    }

    [Fact]
    public void Write_WhenEnabled_AppendsStructuredLocalEntry()
    {
        var path = Path.Combine(_directory, "monihop.log");
        var service = new LocalDiagnosticService(path, () => true);

        service.Write("display.refresh", "failed");

        var text = File.ReadAllText(path);
        Assert.Contains("display.refresh", text, StringComparison.Ordinal);
        Assert.Contains("failed", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_WhenDiagnosticPathCannotBeCreated_DoesNotThrow()
    {
        Directory.CreateDirectory(_directory);
        var blockingFile = Path.Combine(_directory, "not-a-directory");
        File.WriteAllText(blockingFile, "block");
        var service = new LocalDiagnosticService(
            Path.Combine(blockingFile, "monihop.log"),
            () => true);

        var exception = Record.Exception(() => service.Write("display.refresh.failed", "IOException"));

        Assert.Null(exception);
        Assert.True(service.HasWriteFailure);
    }

    [Fact]
    public void Clear_RemovesExistingDiagnosticLog()
    {
        var path = Path.Combine(_directory, "monihop.log");
        Directory.CreateDirectory(_directory);
        File.WriteAllText(path, "entry");
        var service = new LocalDiagnosticService(path, () => true);

        service.Clear();

        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Export_WritesEnvironmentSnapshotWithoutWindowContent()
    {
        var service = new LocalDiagnosticService(
            Path.Combine(_directory, "monihop.log"),
            () => true);
        var target = Path.Combine(_directory, "report.json");
        var snapshot = new DiagnosticSnapshot(
            "0.1.0-alpha1",
            "Windows 11 build 26200",
            "X64",
            "Administrator",
            2,
            true);

        service.Export(target, snapshot);

        var text = File.ReadAllText(target);
        Assert.Contains("0.1.0-alpha1", text, StringComparison.Ordinal);
        Assert.Contains("ConnectedDisplayCount", text, StringComparison.Ordinal);
        Assert.DoesNotContain("WindowTitle", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Export_DoesNotPersistLocalUserOrApplicationPaths()
    {
        var logPath = Path.Combine(_directory, "monihop.log");
        var service = new LocalDiagnosticService(logPath, () => true);
        var target = Path.Combine(_directory, "report.json");
        var snapshot = new DiagnosticSnapshot(
            "0.1.0-alpha1",
            "Windows 11 build 26200",
            "X64",
            "Standard user",
            2,
            true);

        service.Export(target, snapshot);

        var text = File.ReadAllText(target);
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            Assert.DoesNotContain(userProfile, text, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain(AppContext.BaseDirectory, text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(logPath, text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Write_WhenLogExceedsCapacity_KeepsOnlyRecentEntriesWithinOneMegabyte()
    {
        var path = Path.Combine(_directory, "monihop.log");
        var service = new LocalDiagnosticService(path, () => true);

        for (var index = 0; index < 10_000; index++)
        {
            service.Write("window.failed", $"{index:D5}:{new string('x', 160)}");
        }

        var text = File.ReadAllText(path);
        Assert.InRange(new FileInfo(path).Length, 1, 1024 * 1024);
        Assert.Contains("\twindow.failed\t09999:", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\twindow.failed\t00000:", text, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
