using System.Xml.Linq;

namespace MoniHop.Desktop.Tests.Views;

public sealed class AboutPageXamlTests
{
    [Fact]
    public void Page_ContainsProductUpdatesDiagnosticsAndPathSections()
    {
        var path = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "MoniHop.Desktop",
            "Views",
            "AboutPage.xaml");
        var document = XDocument.Load(path);
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var names = document.Descendants()
            .Select(element => (string?)element.Attribute(x + "Name"))
            .Where(name => name is not null)
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(
            4,
            document.Descendants(presentation + "Border")
                .Count(element => ((string?)element.Attribute("Style"))?.Contains(
                    "SurfaceSectionStyle",
                    StringComparison.Ordinal) == true));
        Assert.Contains("ProjectButton", names);
        Assert.Contains("IssuesButton", names);
        Assert.Contains("AutomaticUpdateCheckToggle", names);
        Assert.Contains("CheckUpdatesButton", names);
        Assert.Contains("DetailedDiagnosticsToggle", names);
        Assert.Contains("ExportDiagnosticsButton", names);
        Assert.Contains("ClearDiagnosticsButton", names);
        Assert.Contains("OpenDiagnosticsDirectoryButton", names);
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
