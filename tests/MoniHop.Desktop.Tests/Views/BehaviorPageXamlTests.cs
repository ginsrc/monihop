using System.Xml.Linq;

namespace MoniHop.Desktop.Tests.Views;

public sealed class BehaviorPageXamlTests
{
    [Fact]
    public void Page_ContainsFourFunctionalSectionsAndExpectedControls()
    {
        var document = LoadPage();
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
        Assert.Contains("StartWithWindowsToggle", names);
        Assert.Contains("CloseBehaviorComboBox", names);
        Assert.Contains("CursorLandingComboBox", names);
        Assert.Contains("RecallOffscreenToggle", names);
        Assert.Contains("SuccessNotificationsToggle", names);
        Assert.Contains("RestartElevatedButton", names);
        Assert.Contains("AlwaysAdminToggle", names);
        Assert.Contains("ThemeComboBox", names);
        Assert.Contains("LanguageComboBox", names);
    }

    [Fact]
    public void Page_DoesNotLeaveImplementedControlsGloballyDisabled()
    {
        var source = File.ReadAllText(PagePath());

        Assert.DoesNotContain("待开发", source, StringComparison.Ordinal);
        Assert.DoesNotContain("当前设置不会写入系统", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IsEnabled=\"False\"", source, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource String.GeneralSettings}", source, StringComparison.Ordinal);
    }

    private static XDocument LoadPage() => XDocument.Load(PagePath());

    private static string PagePath() => Path.Combine(
        FindRepositoryRoot(),
        "src",
        "MoniHop.Desktop",
        "Views",
        "BehaviorPage.xaml");

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
