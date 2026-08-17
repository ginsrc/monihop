using System.Xml.Linq;
using MoniHop.Desktop.Settings;
using MoniHop.Desktop.Theming;

namespace MoniHop.Desktop.Tests.Theming;

public sealed class ThemeResourceTests
{
    [Fact]
    public void LightAndDarkDictionaries_HaveIdenticalKeys()
    {
        var light = ReadKeys("Themes/Light.xaml");
        var dark = ReadKeys("Themes/Dark.xaml");

        Assert.NotEmpty(light);
        Assert.Equal(light, dark);
    }

    [Theory]
    [InlineData(AppTheme.Light, false, AppTheme.Light)]
    [InlineData(AppTheme.Dark, true, AppTheme.Dark)]
    [InlineData(AppTheme.System, true, AppTheme.Light)]
    [InlineData(AppTheme.System, false, AppTheme.Dark)]
    public void ResolveTheme_UsesExplicitChoiceOrWindowsPreference(
        AppTheme preference,
        bool windowsUsesLightTheme,
        AppTheme expected)
    {
        Assert.Equal(expected, ThemeService.ResolveTheme(preference, windowsUsesLightTheme));
    }

    private static string[] ReadKeys(string relativePath)
    {
        var root = FindRepositoryRoot();
        var document = XDocument.Load(Path.Combine(root, "src", "MoniHop.Desktop", relativePath));
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        return document.Root!
            .Elements()
            .Select(element => (string?)element.Attribute(x + "Key"))
            .Where(key => key is not null)
            .Cast<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();
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
