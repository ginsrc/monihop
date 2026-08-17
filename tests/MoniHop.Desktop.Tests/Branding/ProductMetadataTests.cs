using System.Reflection;
using System.Xml.Linq;

namespace MoniHop.Desktop.Tests.Branding;

public sealed class ProductMetadataTests
{
    [Fact]
    public void Version_IsResolvedFromAssemblyMetadataAndMatchesProjectVersion()
    {
        var versionField = typeof(ProductInfo).GetField(
            nameof(ProductInfo.Version),
            BindingFlags.Public | BindingFlags.Static);
        var project = XDocument.Load(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "MoniHop.Desktop",
            "MoniHop.Desktop.csproj"));

        Assert.Null(versionField);
        var expectedVersion = project.Descendants("Version").Single().Value;
        Assert.True(
            string.Equals(expectedVersion, ProductInfo.Version, StringComparison.Ordinal),
            $"Expected project version {expectedVersion}, but found {ProductInfo.Version}.");
    }

    [Fact]
    public void GitHubLinks_TargetThePublishedRepository()
    {
        Assert.Equal("https://github.com/ginsrc/monihop", ProductInfo.RepositoryUrl);
        Assert.Equal("https://github.com/ginsrc/monihop/releases", ProductInfo.ReleasesUrl);
        Assert.Equal("https://github.com/ginsrc/monihop/issues", ProductInfo.IssuesUrl);
        Assert.Equal(
            "https://api.github.com/repos/ginsrc/monihop/releases?per_page=20",
            ProductInfo.ReleasesApiUrl);
    }

    [Fact]
    public void MainWindow_DoesNotDuplicateTheVersionOutsideTheAboutPage()
    {
        var document = XDocument.Load(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "MoniHop.Desktop",
            "MainWindow.xaml"));

        Assert.DoesNotContain(
            document.Descendants(),
            element => string.Equals(
                (string?)element.Attribute("Text"),
                "{x:Static local:ProductInfo.Version}",
                StringComparison.Ordinal));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MoniHop.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("MoniHop.sln not found.");
    }
}
