using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using MoniHop.Desktop.Updates;

namespace MoniHop.Desktop.Tests.Updates;

public sealed class UpdateInstallerServiceTests : IDisposable
{
    private readonly string _temporaryDirectory = Path.Combine(
        Path.GetTempPath(),
        "MoniHop.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task DownloadVerifyAndLaunchAsync_ValidChecksumLaunchesInstaller()
    {
        var installerBytes = Encoding.UTF8.GetBytes("installer payload");
        var installerName = "MoniHop-1.0.2-win-x64-setup.exe";
        var checksum = Convert.ToHexString(SHA256.HashData(installerBytes));
        var launcher = new RecordingLauncher();
        using var client = Client(request => request.RequestUri!.AbsolutePath.EndsWith("SHA256SUMS.txt")
            ? Encoding.UTF8.GetBytes($"{checksum}  {installerName}\n")
            : installerBytes);
        var service = new UpdateInstallerService(client, launcher, _temporaryDirectory);

        var result = await service.DownloadVerifyAndLaunchAsync(Package(installerName));

        Assert.True(
            result.Status == UpdateInstallStatus.Started,
            $"{result.Failure}: {result.ErrorMessage}");
        Assert.Equal(Path.Combine(_temporaryDirectory, "1.0.2", installerName), launcher.Path);
        Assert.Equal(installerBytes, await File.ReadAllBytesAsync(launcher.Path!));
    }

    [Fact]
    public async Task DownloadVerifyAndLaunchAsync_ChecksumMismatchDoesNotLaunchInstaller()
    {
        var versionDirectory = Path.Combine(_temporaryDirectory, "1.0.2");
        Directory.CreateDirectory(versionDirectory);
        var existingInstallerPath = Path.Combine(versionDirectory, "MoniHop-1.0.2-win-x64-setup.exe");
        await File.WriteAllTextAsync(existingInstallerPath, "previous verified installer");
        var launcher = new RecordingLauncher();
        using var client = Client(request => request.RequestUri!.AbsolutePath.EndsWith("SHA256SUMS.txt")
            ? Encoding.UTF8.GetBytes($"{new string('0', 64)}  MoniHop-1.0.2-win-x64-setup.exe\n")
            : Encoding.UTF8.GetBytes("installer payload"));
        var service = new UpdateInstallerService(client, launcher, _temporaryDirectory);

        var result = await service.DownloadVerifyAndLaunchAsync(Package());

        Assert.Equal(UpdateInstallStatus.Failed, result.Status);
        Assert.Equal(UpdateInstallFailure.ChecksumMismatch, result.Failure);
        Assert.Null(launcher.Path);
        Assert.Equal("previous verified installer", await File.ReadAllTextAsync(existingInstallerPath));
    }

    [Fact]
    public async Task DownloadVerifyAndLaunchAsync_MissingChecksumEntryDoesNotLaunchInstaller()
    {
        var launcher = new RecordingLauncher();
        using var client = Client(request => request.RequestUri!.AbsolutePath.EndsWith("SHA256SUMS.txt")
            ? Encoding.UTF8.GetBytes($"{new string('A', 64)}  another-file.exe\n")
            : Encoding.UTF8.GetBytes("installer payload"));
        var service = new UpdateInstallerService(client, launcher, _temporaryDirectory);

        var result = await service.DownloadVerifyAndLaunchAsync(Package());

        Assert.Equal(UpdateInstallFailure.ChecksumMissing, result.Failure);
        Assert.Null(launcher.Path);
    }

    [Fact]
    public async Task DownloadVerifyAndLaunchAsync_NetworkFailureReturnsFailureAndDoesNotLaunch()
    {
        var launcher = new RecordingLauncher();
        using var client = new HttpClient(new StubHandler(_ => throw new HttpRequestException("offline")));
        var service = new UpdateInstallerService(client, launcher, _temporaryDirectory);

        var result = await service.DownloadVerifyAndLaunchAsync(Package());

        Assert.Equal(UpdateInstallFailure.DownloadFailed, result.Failure);
        Assert.Null(launcher.Path);
    }

    [Fact]
    public async Task DownloadVerifyAndLaunchAsync_NonHttpsAssetIsRejectedBeforeDownload()
    {
        var requestCount = 0;
        var launcher = new RecordingLauncher();
        using var client = Client(_ =>
        {
            requestCount++;
            return [];
        });
        var package = Package() with
        {
            InstallerUri = new Uri("http://example.test/MoniHop-1.0.2-win-x64-setup.exe"),
        };
        var service = new UpdateInstallerService(client, launcher, _temporaryDirectory);

        var result = await service.DownloadVerifyAndLaunchAsync(package);

        Assert.Equal(UpdateInstallFailure.InvalidPackage, result.Failure);
        Assert.Equal(0, requestCount);
        Assert.Null(launcher.Path);
    }

    [Fact]
    public async Task DownloadVerifyAndLaunchAsync_UnsafeVersionDirectoryIsRejectedBeforeDownload()
    {
        var requestCount = 0;
        var launcher = new RecordingLauncher();
        using var client = Client(_ =>
        {
            requestCount++;
            return [];
        });
        var package = new UpdateInstallerPackage(
            "..",
            "MoniHop-..-win-x64-setup.exe",
            new Uri("https://example.test/installer.exe"),
            "SHA256SUMS.txt",
            new Uri("https://example.test/SHA256SUMS.txt"));
        var service = new UpdateInstallerService(client, launcher, _temporaryDirectory);

        var result = await service.DownloadVerifyAndLaunchAsync(package);

        Assert.Equal(UpdateInstallFailure.InvalidPackage, result.Failure);
        Assert.Equal(0, requestCount);
    }

    [Fact]
    public async Task DownloadVerifyAndLaunchAsync_LaunchFailureIsReported()
    {
        var installerBytes = Encoding.UTF8.GetBytes("installer payload");
        var checksum = Convert.ToHexString(SHA256.HashData(installerBytes));
        using var client = Client(request => request.RequestUri!.AbsolutePath.EndsWith("SHA256SUMS.txt")
            ? Encoding.UTF8.GetBytes($"{checksum}  MoniHop-1.0.2-win-x64-setup.exe\n")
            : installerBytes);
        var service = new UpdateInstallerService(client, new ThrowingLauncher(), _temporaryDirectory);

        var result = await service.DownloadVerifyAndLaunchAsync(Package());

        Assert.Equal(UpdateInstallFailure.LaunchFailed, result.Failure);
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }

    private static UpdateInstallerPackage Package(
        string installerName = "MoniHop-1.0.2-win-x64-setup.exe") => new(
        "1.0.2",
        installerName,
        new Uri($"https://example.test/{installerName}"),
        "SHA256SUMS.txt",
        new Uri("https://example.test/SHA256SUMS.txt"));

    private static HttpClient Client(Func<HttpRequestMessage, byte[]> content) => new(
        new StubHandler(request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(content(request)),
        }));

    private sealed class RecordingLauncher : IUpdateInstallerLauncher
    {
        public string? Path { get; private set; }

        public void Launch(string installerPath) => Path = installerPath;
    }

    private sealed class ThrowingLauncher : IUpdateInstallerLauncher
    {
        public void Launch(string installerPath) => throw new InvalidOperationException("cannot launch");
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(handler(request));
    }
}
