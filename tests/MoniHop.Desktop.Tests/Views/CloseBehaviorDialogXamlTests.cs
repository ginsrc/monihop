using System.Xml.Linq;

namespace MoniHop.Desktop.Tests.Views;

public sealed class CloseBehaviorDialogXamlTests
{
    [Fact]
    public void Dialog_DefaultsToTrayAndOffersRememberChoice()
    {
        var path = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "MoniHop.Desktop",
            "Views",
            "CloseBehaviorDialog.xaml");
        var document = XDocument.Load(path);
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var elements = document.Descendants().ToArray();
        var byName = elements
            .Where(element => element.Attribute(x + "Name") is not null)
            .ToDictionary(element => (string)element.Attribute(x + "Name")!, StringComparer.Ordinal);

        Assert.Contains("RememberChoiceCheckBox", byName.Keys);
        Assert.Equal("True", (string?)byName["MinimizeButton"].Attribute("IsDefault"));
        Assert.Contains("ExitButton", byName.Keys);
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
