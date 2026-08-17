using System.Xml.Linq;

namespace MoniHop.Desktop.Tests.Licensing;

public sealed class LicenseMetadataTests
{
    [Fact]
    public void RepositoryLicense_IsStandardMitForGinsrc()
    {
        var root = FindRepositoryRoot();
        var text = File.ReadAllText(Path.Combine(root, "LICENSE"));

        Assert.StartsWith("MIT License", text, StringComparison.Ordinal);
        Assert.Contains("Copyright (c) 2026 ginsrc", text, StringComparison.Ordinal);
        Assert.Contains("Permission is hereby granted, free of charge", text, StringComparison.Ordinal);
        Assert.Contains("THE SOFTWARE IS PROVIDED \"AS IS\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("your name", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AboutPage_ShowsMitAndDoesNotShowUndecidedLicense()
    {
        var root = FindRepositoryRoot();
        var path = Path.Combine(root, "src", "MoniHop.Desktop", "Views", "AboutPage.xaml");
        var source = File.ReadAllText(path);
        var document = XDocument.Load(path);
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var names = document.Descendants()
            .Select(element => (string?)element.Attribute(x + "Name"))
            .Where(name => name is not null)
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("LicenseButton", names);
        Assert.Contains("MIT License", source, StringComparison.Ordinal);
        Assert.DoesNotContain("待确定", source, StringComparison.Ordinal);
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
