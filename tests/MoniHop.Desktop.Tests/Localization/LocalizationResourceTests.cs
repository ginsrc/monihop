using System.Globalization;
using System.Xml.Linq;
using MoniHop.Desktop.Localization;
using MoniHop.Desktop.Settings;

namespace MoniHop.Desktop.Tests.Localization;

public sealed class LocalizationResourceTests
{
    [Fact]
    public void ChineseAndEnglishDictionaries_HaveIdenticalKeys()
    {
        var chinese = ReadKeys("Localization/Strings.zh-CN.xaml");
        var english = ReadKeys("Localization/Strings.en-US.xaml");

        Assert.NotEmpty(chinese);
        Assert.Equal(chinese, english);
    }

    [Theory]
    [InlineData(AppLanguage.SimplifiedChinese, "en-US", AppLanguage.SimplifiedChinese)]
    [InlineData(AppLanguage.English, "zh-CN", AppLanguage.English)]
    [InlineData(AppLanguage.System, "zh-CN", AppLanguage.SimplifiedChinese)]
    [InlineData(AppLanguage.System, "zh-SG", AppLanguage.SimplifiedChinese)]
    [InlineData(AppLanguage.System, "en-US", AppLanguage.English)]
    [InlineData(AppLanguage.System, "fr-FR", AppLanguage.English)]
    public void ResolveLanguage_UsesExplicitChoiceOrSupportedSystemFallback(
        AppLanguage preference,
        string cultureName,
        AppLanguage expected)
    {
        Assert.Equal(
            expected,
            LocalizationService.ResolveLanguage(preference, CultureInfo.GetCultureInfo(cultureName)));
    }

    [Fact]
    public void ResolveLanguage_InvariantCultureFallsBackToSimplifiedChinese()
    {
        Assert.Equal(
            AppLanguage.SimplifiedChinese,
            LocalizationService.ResolveLanguage(AppLanguage.System, CultureInfo.InvariantCulture));
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
