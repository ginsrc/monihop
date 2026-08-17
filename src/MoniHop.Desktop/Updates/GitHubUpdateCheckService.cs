using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MoniHop.Desktop.Updates;

public enum UpdateCheckStatus
{
    UpToDate,
    UpdateAvailable,
    NoRelease,
    Failed,
}

public sealed record UpdateInstallerPackage(
    string Version,
    string InstallerFileName,
    Uri InstallerUri,
    string ChecksumFileName,
    Uri ChecksumUri);

public sealed record UpdateCheckResult(
    UpdateCheckStatus Status,
    string? LatestVersion = null,
    Uri? ReleaseUri = null,
    string? ErrorMessage = null,
    UpdateInstallerPackage? InstallerPackage = null);

public sealed class GitHubUpdateCheckService
{
    private static readonly Uri ReleasesUri = new(ProductInfo.ReleasesApiUrl);
    private readonly HttpClient _client;
    private readonly string _currentVersion;

    public GitHubUpdateCheckService(HttpClient client, string currentVersion)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        ArgumentException.ThrowIfNullOrWhiteSpace(currentVersion);
        _currentVersion = currentVersion;
    }

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ReleasesUri);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("MoniHop", _currentVersion));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
            using var response = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new UpdateCheckResult(UpdateCheckStatus.NoRelease);
            }

            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (!SemanticVersion.TryParse(_currentVersion, out var current))
            {
                return new UpdateCheckResult(UpdateCheckStatus.Failed, ErrorMessage: "Invalid release metadata.");
            }

            var releases = document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement.EnumerateArray()
                : new[] { document.RootElement }.AsEnumerable();
            var candidates = releases
                .Where(release => !release.TryGetProperty("draft", out var draft) || !draft.GetBoolean())
                .Where(release => current.Prerelease is not null ||
                    !release.TryGetProperty("prerelease", out var prerelease) || !prerelease.GetBoolean())
                .Select(ParseRelease)
                .Where(release => release is not null)
                .Select(release => release!)
                .OrderByDescending(release => release.Version)
                .ToArray();
            if (candidates.Length == 0)
            {
                return new UpdateCheckResult(UpdateCheckStatus.NoRelease);
            }

            var latest = candidates[0];
            return latest.Version.CompareTo(current) > 0
                ? new UpdateCheckResult(
                    UpdateCheckStatus.UpdateAvailable,
                    latest.Version.Display,
                    latest.Uri,
                    InstallerPackage: latest.InstallerPackage)
                : new UpdateCheckResult(UpdateCheckStatus.UpToDate, current.Display, latest.Uri);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
        {
            return new UpdateCheckResult(UpdateCheckStatus.Failed, ErrorMessage: exception.Message);
        }
    }

    private static ReleaseMetadata? ParseRelease(JsonElement release)
    {
        var tag = release.TryGetProperty("tag_name", out var tagElement) ? tagElement.GetString() : null;
        var releaseUrl = release.TryGetProperty("html_url", out var uriElement) ? uriElement.GetString() : null;
        return SemanticVersion.TryParse(tag, out var version) &&
            Uri.TryCreate(releaseUrl, UriKind.Absolute, out var uri)
            ? new ReleaseMetadata(version, uri, ParseInstallerPackage(release, version))
            : null;
    }

    private static UpdateInstallerPackage? ParseInstallerPackage(JsonElement release, SemanticVersion version)
    {
        if (!release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var installerFileName = $"MoniHop-{version.Display}-win-x64-setup.exe";
        const string checksumFileName = "SHA256SUMS.txt";
        string? installerUrl = null;
        string? checksumUrl = null;
        foreach (var asset in assets.EnumerateArray())
        {
            if (!asset.TryGetProperty("name", out var nameElement) ||
                !asset.TryGetProperty("browser_download_url", out var urlElement))
            {
                continue;
            }

            var name = nameElement.GetString();
            var url = urlElement.GetString();
            if (string.Equals(name, installerFileName, StringComparison.Ordinal))
            {
                installerUrl = url;
            }
            else if (string.Equals(name, checksumFileName, StringComparison.Ordinal))
            {
                checksumUrl = url;
            }
        }

        return Uri.TryCreate(installerUrl, UriKind.Absolute, out var installerUri) &&
            Uri.TryCreate(checksumUrl, UriKind.Absolute, out var checksumUri) &&
            installerUri.Scheme == Uri.UriSchemeHttps &&
            checksumUri.Scheme == Uri.UriSchemeHttps
            ? new UpdateInstallerPackage(
                version.Display,
                installerFileName,
                installerUri,
                checksumFileName,
                checksumUri)
            : null;
    }

    private sealed record ReleaseMetadata(
        SemanticVersion Version,
        Uri Uri,
        UpdateInstallerPackage? InstallerPackage);

    private sealed record SemanticVersion(Version Core, string? Prerelease) : IComparable<SemanticVersion>
    {
        public string Display => Core + (Prerelease is null ? string.Empty : "-" + Prerelease);

        public int CompareTo(SemanticVersion? other)
        {
            if (other is null)
            {
                return 1;
            }

            var coreComparison = Core.CompareTo(other.Core);
            if (coreComparison != 0)
            {
                return coreComparison;
            }

            if (Prerelease is null && other.Prerelease is not null)
            {
                return 1;
            }

            if (Prerelease is not null && other.Prerelease is null)
            {
                return -1;
            }

            if (Prerelease is null)
            {
                return 0;
            }

            var left = Prerelease!.Split('.');
            var right = other.Prerelease!.Split('.');
            for (var index = 0; index < Math.Min(left.Length, right.Length); index++)
            {
                var leftNumeric = int.TryParse(left[index], out var leftNumber);
                var rightNumeric = int.TryParse(right[index], out var rightNumber);
                var comparison = leftNumeric && rightNumeric
                    ? leftNumber.CompareTo(rightNumber)
                    : leftNumeric != rightNumeric
                        ? leftNumeric ? -1 : 1
                        : string.Compare(left[index], right[index], StringComparison.OrdinalIgnoreCase);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return left.Length.CompareTo(right.Length);
        }

        public static bool TryParse(string? value, out SemanticVersion version)
        {
            version = null!;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var normalized = value.Trim().TrimStart('v', 'V').Split('+', 2)[0];
            var parts = normalized.Split('-', 2);
            if (!Version.TryParse(parts[0], out var core))
            {
                return false;
            }

            version = new SemanticVersion(core, parts.Length == 2 ? parts[1] : null);
            return true;
        }
    }
}
