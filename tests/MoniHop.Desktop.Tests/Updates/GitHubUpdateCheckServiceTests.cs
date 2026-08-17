using System.Net;
using System.Net.Http;
using System.Text;
using MoniHop.Desktop.Updates;

namespace MoniHop.Desktop.Tests.Updates;

public sealed class GitHubUpdateCheckServiceTests
{
    [Fact]
    public async Task CheckAsync_UsesThePublishedRepositoryReleasesEndpoint()
    {
        Uri? requestedUri = null;
        using var client = new HttpClient(new StubHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json"),
            };
        }));
        var service = new GitHubUpdateCheckService(client, "1.0.0");

        await service.CheckAsync();

        Assert.Equal(
            new Uri("https://api.github.com/repos/ginsrc/monihop/releases?per_page=20"),
            requestedUri);
    }

    [Fact]
    public async Task CheckAsync_LatestStableReleaseIsNewerThanMatchingAlpha()
    {
        var client = Client(HttpStatusCode.OK, """
            { "tag_name": "v0.1.0", "html_url": "https://github.com/ginsrc/monihop/releases/tag/v0.1.0" }
            """);
        var service = new GitHubUpdateCheckService(client, "0.1.0-alpha1");

        var result = await service.CheckAsync();

        Assert.Equal(UpdateCheckStatus.UpdateAvailable, result.Status);
        Assert.Equal("0.1.0", result.LatestVersion);
        Assert.Equal(
            new Uri("https://github.com/ginsrc/monihop/releases/tag/v0.1.0"),
            result.ReleaseUri);
    }

    [Fact]
    public async Task CheckAsync_ParsesMatchingInstallerAndChecksumAssets()
    {
        var client = Client(HttpStatusCode.OK, """
            {
              "tag_name": "v1.0.2",
              "html_url": "https://github.com/ginsrc/monihop/releases/tag/v1.0.2",
              "assets": [
                {
                  "name": "MoniHop-1.0.2-win-x64-setup.exe",
                  "browser_download_url": "https://github.com/ginsrc/monihop/releases/download/v1.0.2/MoniHop-1.0.2-win-x64-setup.exe"
                },
                {
                  "name": "SHA256SUMS.txt",
                  "browser_download_url": "https://github.com/ginsrc/monihop/releases/download/v1.0.2/SHA256SUMS.txt"
                }
              ]
            }
            """);
        var service = new GitHubUpdateCheckService(client, "1.0.1");

        var result = await service.CheckAsync();

        Assert.Equal(UpdateCheckStatus.UpdateAvailable, result.Status);
        Assert.NotNull(result.InstallerPackage);
        Assert.Equal(
            "MoniHop-1.0.2-win-x64-setup.exe",
            result.InstallerPackage!.InstallerFileName);
        Assert.Equal(
            new Uri("https://github.com/ginsrc/monihop/releases/download/v1.0.2/MoniHop-1.0.2-win-x64-setup.exe"),
            result.InstallerPackage.InstallerUri);
        Assert.Equal(
            new Uri("https://github.com/ginsrc/monihop/releases/download/v1.0.2/SHA256SUMS.txt"),
            result.InstallerPackage.ChecksumUri);
    }

    [Fact]
    public async Task CheckAsync_MissingAssetsLeavesInstallerPackageUnavailable()
    {
        var client = Client(HttpStatusCode.OK, """
            { "tag_name": "v1.0.2", "html_url": "https://github.com/ginsrc/monihop/releases/tag/v1.0.2", "assets": [] }
            """);
        var service = new GitHubUpdateCheckService(client, "1.0.1");

        var result = await service.CheckAsync();

        Assert.Equal(UpdateCheckStatus.UpdateAvailable, result.Status);
        Assert.Null(result.InstallerPackage);
    }

    [Fact]
    public async Task CheckAsync_SameStableVersionIsUpToDate()
    {
        var client = Client(HttpStatusCode.OK, """
            { "tag_name": "v0.1.0", "html_url": "https://github.com/ginsrc/monihop/releases/tag/v0.1.0" }
            """);
        var service = new GitHubUpdateCheckService(client, "0.1.0");

        var result = await service.CheckAsync();

        Assert.Equal(UpdateCheckStatus.UpToDate, result.Status);
    }

    [Fact]
    public async Task CheckAsync_NoPublishedReleaseReturnsNoRelease()
    {
        var service = new GitHubUpdateCheckService(
            Client(HttpStatusCode.NotFound, "{}"),
            "0.1.0-alpha1");

        var result = await service.CheckAsync();

        Assert.Equal(UpdateCheckStatus.NoRelease, result.Status);
    }

    [Fact]
    public async Task CheckAsync_NetworkFailureReturnsFailedInsteadOfThrowing()
    {
        var service = new GitHubUpdateCheckService(
            new HttpClient(new StubHandler(_ => throw new HttpRequestException("offline"))),
            "0.1.0-alpha1");

        var result = await service.CheckAsync();

        Assert.Equal(UpdateCheckStatus.Failed, result.Status);
        Assert.Equal("offline", result.ErrorMessage);
    }

    [Fact]
    public async Task CheckAsync_StableChannelIgnoresPrereleaseAndUsesNumericPrereleaseOrdering()
    {
        var client = Client(HttpStatusCode.OK, """
            [
              { "tag_name": "v1.0.0-alpha10", "html_url": "https://github.com/ginsrc/monihop/releases/tag/v1.0.0-alpha10", "prerelease": true },
              { "tag_name": "v1.0.0", "html_url": "https://github.com/ginsrc/monihop/releases/tag/v1.0.0", "prerelease": false }
            ]
            """);
        var service = new GitHubUpdateCheckService(client, "1.0.0");

        var result = await service.CheckAsync();

        Assert.Equal(UpdateCheckStatus.UpToDate, result.Status);
        Assert.Equal("1.0.0", result.LatestVersion);
    }

    private static HttpClient Client(HttpStatusCode statusCode, string content) => new(
        new StubHandler(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json"),
        }));

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(response(request));
    }
}
