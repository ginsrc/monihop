using System.Xml.Linq;

namespace MoniHop.Desktop.Tests.Theming;

public sealed class TypographyResourceTests
{
    public static TheoryData<string> FormalPages => new()
    {
        "DisplaysPage.xaml",
        "ProjectionPage.xaml",
        "ApplicationProjectionPage.xaml",
        "HotKeysPage.xaml",
        "BehaviorPage.xaml",
        "AboutPage.xaml",
    };

    public static TheoryData<string> FormalDialogs => new()
    {
        "CloseBehaviorDialog.xaml",
        "DisplayRenameDialog.xaml",
        "RunningApplicationDialog.xaml",
        "InstalledApplicationDialog.xaml",
    };

    [Fact]
    public void Controls_DefinesUnifiedTypographyScale()
    {
        var document = XDocument.Load(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "MoniHop.Desktop",
            "Themes",
            "Controls.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var styles = document.Descendants(presentation + "Style")
            .Where(style => style.Attribute(x + "Key") is not null)
            .ToDictionary(
                style => (string)style.Attribute(x + "Key")!,
                StringComparer.Ordinal);

        AssertStyle(styles, "BrandTitleStyle", "20", "SemiBold");
        AssertStyle(styles, "PageTitleStyle", "24", "SemiBold");
        AssertStyle(styles, "PageSubtitleStyle", "14", null);
        AssertStyle(styles, "SectionTitleStyle", "18", "SemiBold");
        AssertStyle(styles, "CardTitleStyle", "16", "SemiBold");
        AssertStyle(styles, "FieldLabelStyle", "14", "SemiBold");
        AssertStyle(styles, "BodyTextStyle", "14", null);
        AssertStyle(styles, "SecondaryTextStyle", "13", null);
        AssertStyle(styles, "CaptionTextStyle", "12", null);
    }

    [Theory]
    [MemberData(nameof(FormalPages))]
    public void FormalPage_UsesSharedTypographyInsteadOfLocalTextSizes(string fileName)
    {
        var document = XDocument.Load(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "MoniHop.Desktop",
            "Views",
            fileName));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        Assert.Null(document.Root?.Attribute("FontSize"));

        var textBlocks = document.Descendants(presentation + "TextBlock").ToArray();
        Assert.Contains(
            textBlocks,
            text => HasStyle(text, "PageTitleStyle"));
        Assert.Contains(
            textBlocks,
            text => HasStyle(text, "PageSubtitleStyle"));
        Assert.DoesNotContain(textBlocks, text =>
            !IsIcon(text)
            && text.Attribute("FontSize") is not null);
        Assert.DoesNotContain(textBlocks, text =>
            !IsIcon(text)
            && text.Attribute("FontWeight") is not null
            && text.Attribute("Style") is null);
    }

    [Theory]
    [MemberData(nameof(FormalDialogs))]
    public void FormalDialog_UsesSharedTypographyInsteadOfLocalTextSizes(string fileName)
    {
        var document = XDocument.Load(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "MoniHop.Desktop",
            "Views",
            fileName));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        var textBlocks = document.Descendants(presentation + "TextBlock").ToArray();
        Assert.Contains(
            textBlocks,
            text => HasStyle(text, "SectionTitleStyle"));
        Assert.DoesNotContain(textBlocks, text =>
            !IsIcon(text)
            && text.Attribute("FontSize") is not null);
        Assert.DoesNotContain(textBlocks, text =>
            !IsIcon(text)
            && text.Attribute("FontWeight") is not null
            && text.Attribute("Style") is null);
    }

    [Fact]
    public void MainWindow_UsesBrandTypographyToken()
    {
        var document = XDocument.Load(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "MoniHop.Desktop",
            "MainWindow.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        var textBlocks = document.Descendants(presentation + "TextBlock").ToArray();

        Assert.Contains(textBlocks, text => HasStyle(text, "BrandTitleStyle"));
        Assert.DoesNotContain(textBlocks, text =>
            !IsIcon(text)
            && (text.Attribute("FontSize") is not null || text.Attribute("FontWeight") is not null));
    }

    [Fact]
    public void Navigation_UsesCompactBrandAndExplicitSelectionStates()
    {
        var root = FindRepositoryRoot();
        var window = XDocument.Load(Path.Combine(
            root,
            "src",
            "MoniHop.Desktop",
            "MainWindow.xaml"));
        var controls = XDocument.Load(Path.Combine(
            root,
            "src",
            "MoniHop.Desktop",
            "Themes",
            "Controls.xaml"));
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";

        var brandMark = Assert.Single(
            window.Descendants(presentation + "Viewbox"),
            viewbox => string.Equals(
                (string?)viewbox.Attribute(x + "Name"),
                "BrandMark",
                StringComparison.Ordinal));
        Assert.Equal("38", (string?)brandMark.Attribute("Width"));
        Assert.Equal("28", (string?)brandMark.Attribute("Height"));
        Assert.Contains(
            brandMark.Descendants(presentation + "Canvas"),
            canvas => string.Equals(
                (string?)canvas.Attribute(x + "Name"),
                "BrandMarkViewport",
                StringComparison.Ordinal));
        Assert.True(brandMark.Descendants(presentation + "Path").Count() >= 4);
        Assert.DoesNotContain(
            window.Descendants(presentation + "Image"),
            image => string.Equals(
                (string?)image.Attribute("Source"),
                "Assets/MoniHop.ico",
                StringComparison.Ordinal));

        var navigationStyle = controls.Descendants(presentation + "Style")
            .Single(style => string.Equals(
                (string?)style.Attribute(x + "Key"),
                "NavigationItemStyle",
                StringComparison.Ordinal));
        Assert.Contains(
            navigationStyle.Elements(presentation + "Setter"),
            setter => string.Equals((string?)setter.Attribute("Property"), "FocusVisualStyle", StringComparison.Ordinal)
                && string.Equals((string?)setter.Attribute("Value"), "{x:Null}", StringComparison.Ordinal));
        Assert.Contains(
            navigationStyle.Descendants(),
            element => string.Equals((string?)element.Attribute(x + "Name"), "SelectionIndicator", StringComparison.Ordinal));
        Assert.Contains(
            navigationStyle.Descendants(),
            element => string.Equals((string?)element.Attribute(x + "Name"), "GroupDivider", StringComparison.Ordinal));
        Assert.Contains(
            navigationStyle.Descendants(presentation + "DataTrigger"),
            trigger => string.Equals((string?)trigger.Attribute("Value"), "通用设置", StringComparison.Ordinal));
    }

    private static void AssertStyle(
        IReadOnlyDictionary<string, XElement> styles,
        string key,
        string fontSize,
        string? fontWeight)
    {
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        var style = Assert.Contains(key, styles);
        var setters = style.Elements(presentation + "Setter")
            .ToDictionary(
                setter => (string?)setter.Attribute("Property") ?? string.Empty,
                setter => (string?)setter.Attribute("Value"),
                StringComparer.Ordinal);

        Assert.Equal(fontSize, setters["FontSize"]);
        if (fontWeight is not null)
        {
            Assert.Equal(fontWeight, setters["FontWeight"]);
        }
    }

    private static bool HasStyle(XElement text, string styleName) =>
        ((string?)text.Attribute("Style"))?.Contains(styleName, StringComparison.Ordinal) == true;

    private static bool IsIcon(XElement text) =>
        string.Equals(
            (string?)text.Attribute("FontFamily"),
            "Segoe Fluent Icons",
            StringComparison.Ordinal);

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
