using System.Xml.Linq;

namespace MoniHop.Desktop.Tests.WindowProjection;

public sealed class QuickProjectionWindowXamlTests
{
    [Fact]
    public void LayoutList_DisablesHorizontalScrollBar()
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(
            root,
            "src",
            "MoniHop.Desktop",
            "WindowProjection",
            "QuickProjectionWindow.xaml"));
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var layoutList = document.Descendants()
            .Single(element => (string?)element.Attribute(x + "Name") == "LayoutList");

        Assert.Equal(
            "Disabled",
            (string?)layoutList.Attribute("ScrollViewer.HorizontalScrollBarVisibility"));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MoniHop.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("MoniHop repository root was not found.");
    }
}
