using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;

namespace MoniHop.Desktop.Updates;

public enum UpdateInstallStatus
{
    Started,
    Failed,
}

public enum UpdateInstallFailure
{
    None,
    InvalidPackage,
    DownloadFailed,
    ChecksumMissing,
    ChecksumMismatch,
    LaunchFailed,
}

public sealed record UpdateInstallResult(
    UpdateInstallStatus Status,
    UpdateInstallFailure Failure = UpdateInstallFailure.None,
    string? ErrorMessage = null)
{
    public static UpdateInstallResult Started() => new(UpdateInstallStatus.Started);

    public static UpdateInstallResult Failed(UpdateInstallFailure failure, string? message = null) =>
        new(UpdateInstallStatus.Failed, failure, message);
}

public interface IUpdateInstallerLauncher
{
    void Launch(string installerPath);
}

public sealed class NativeUpdateInstallerLauncher : IUpdateInstallerLauncher
{
    public void Launch(string installerPath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = installerPath,
            UseShellExecute = true,
        });
    }
}

public sealed class UpdateInstallerService
{
    private readonly HttpClient _client;
    private readonly IUpdateInstallerLauncher _launcher;
    private readonly string _downloadRoot;

    public UpdateInstallerService(
        HttpClient client,
        IUpdateInstallerLauncher launcher,
        string? downloadRoot = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _launcher = launcher ?? throw new ArgumentNullException(nameof(launcher));
        _downloadRoot = Path.GetFullPath(downloadRoot ?? Path.Combine(Path.GetTempPath(), "MoniHop", "updates"));
    }

    public async Task<UpdateInstallResult> DownloadVerifyAndLaunchAsync(
        UpdateInstallerPackage package,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);
        if (!IsValidHttpsUri(package.InstallerUri) ||
            !IsValidHttpsUri(package.ChecksumUri) ||
            !IsSafeVersion(package.Version) ||
            !IsSafeFileName(package.InstallerFileName) ||
            !string.Equals(
                package.InstallerFileName,
                $"MoniHop-{package.Version}-win-x64-setup.exe",
                StringComparison.Ordinal) ||
            !string.Equals(package.ChecksumFileName, "SHA256SUMS.txt", StringComparison.Ordinal))
        {
            return UpdateInstallResult.Failed(UpdateInstallFailure.InvalidPackage);
        }

        var versionDirectory = Path.Combine(_downloadRoot, package.Version);
        var installerPath = Path.Combine(versionDirectory, package.InstallerFileName);
        var checksumPath = Path.Combine(versionDirectory, package.ChecksumFileName);
        var installerTemporaryPath = installerPath + ".download";
        var checksumTemporaryPath = checksumPath + ".download";
        try
        {
            Directory.CreateDirectory(versionDirectory);
            await DownloadFileAsync(package.InstallerUri, installerTemporaryPath, cancellationToken).ConfigureAwait(false);
            await DownloadFileAsync(package.ChecksumUri, checksumTemporaryPath, cancellationToken).ConfigureAwait(false);

            var expectedHash = await FindExpectedHashAsync(
                    checksumTemporaryPath,
                    package.InstallerFileName,
                    cancellationToken)
                .ConfigureAwait(false);
            if (expectedHash is null)
            {
                return UpdateInstallResult.Failed(UpdateInstallFailure.ChecksumMissing);
            }

            string actualHash;
            await using (var installerStream = File.OpenRead(installerTemporaryPath))
            {
                actualHash = Convert.ToHexString(await SHA256.HashDataAsync(installerStream, cancellationToken)
                    .ConfigureAwait(false));
            }

            if (!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
            {
                return UpdateInstallResult.Failed(UpdateInstallFailure.ChecksumMismatch);
            }

            File.Move(installerTemporaryPath, installerPath, overwrite: true);
            File.Move(checksumTemporaryPath, checksumPath, overwrite: true);
            try
            {
                _launcher.Launch(installerPath);
            }
            catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
            {
                return UpdateInstallResult.Failed(UpdateInstallFailure.LaunchFailed, exception.Message);
            }

            return UpdateInstallResult.Started();
        }
        catch (HttpRequestException exception)
        {
            return UpdateInstallResult.Failed(UpdateInstallFailure.DownloadFailed, exception.Message);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return UpdateInstallResult.Failed(UpdateInstallFailure.DownloadFailed, exception.Message);
        }
        catch (IOException exception)
        {
            return UpdateInstallResult.Failed(UpdateInstallFailure.DownloadFailed, exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return UpdateInstallResult.Failed(UpdateInstallFailure.DownloadFailed, exception.Message);
        }
        finally
        {
            TryDelete(installerTemporaryPath);
            TryDelete(checksumTemporaryPath);
        }
    }

    private async Task DownloadFileAsync(Uri uri, string destinationPath, CancellationToken cancellationToken)
    {
        using var response = await _client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var destination = File.Create(destinationPath);
        await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string?> FindExpectedHashAsync(
        string checksumPath,
        string installerFileName,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(checksumPath);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            var fields = line.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length >= 2 &&
                string.Equals(fields[1].TrimStart('*'), installerFileName, StringComparison.Ordinal))
            {
                return fields[0].Length == 64 && fields[0].All(Uri.IsHexDigit) ? fields[0] : null;
            }
        }

        return null;
    }

    private static bool IsValidHttpsUri(Uri uri) =>
        uri.IsAbsoluteUri && uri.Scheme == Uri.UriSchemeHttps;

    private static bool IsSafeFileName(string fileName) =>
        !string.IsNullOrWhiteSpace(fileName) &&
        string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal) &&
        fileName.EndsWith("-win-x64-setup.exe", StringComparison.Ordinal);

    private static bool IsSafeVersion(string version) =>
        !string.IsNullOrWhiteSpace(version) &&
        version is not "." and not ".." &&
        version.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-');

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A later update attempt overwrites the fixed temporary path.
        }
    }
}
