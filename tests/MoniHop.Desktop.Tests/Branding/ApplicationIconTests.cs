using System.Xml.Linq;

namespace MoniHop.Desktop.Tests.Branding;

public sealed class ApplicationIconTests
{
    private const string IconRelativePath = "Assets\\MoniHop.ico";
    private const string WindowIconRelativePath = "Assets\\MoniHop.Window.ico";

    [Fact]
    public void Icon_ContainsWindowsDesktopSizes()
    {
        var path = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "MoniHop.Desktop",
            "Assets",
            "MoniHop.ico");

        Assert.True(File.Exists(path), $"Application icon not found: {path}");

        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        Assert.Equal(0, reader.ReadUInt16());
        Assert.Equal(1, reader.ReadUInt16());

        var imageCount = reader.ReadUInt16();
        var sizes = new HashSet<int>();
        for (var index = 0; index < imageCount; index++)
        {
            var width = reader.ReadByte();
            var height = reader.ReadByte();
            reader.ReadBytes(14);

            var normalizedWidth = width == 0 ? 256 : width;
            var normalizedHeight = height == 0 ? 256 : height;
            Assert.Equal(normalizedWidth, normalizedHeight);
            sizes.Add(normalizedWidth);
        }

        Assert.Subset(sizes, new HashSet<int> { 16, 20, 24, 32, 40, 48, 64, 128, 256 });
    }

    [Fact]
    public void DesktopProject_UsesIconForExecutableAndWpfResources()
    {
        var projectPath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "MoniHop.Desktop",
            "MoniHop.Desktop.csproj");
        var project = XDocument.Load(projectPath);

        Assert.Equal(
            IconRelativePath,
            project.Descendants("ApplicationIcon").Single().Value);
        Assert.Contains(
            project.Descendants("Resource"),
            resource => string.Equals(
                (string?)resource.Attribute("Include"),
                IconRelativePath,
                StringComparison.Ordinal));
        Assert.Contains(
            project.Descendants("Resource"),
            resource => string.Equals(
                (string?)resource.Attribute("Include"),
                WindowIconRelativePath,
                StringComparison.Ordinal));
    }

    [Fact]
    public void MainWindow_UsesTitleBarOptimizedIcon()
    {
        var windowPath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "MoniHop.Desktop",
            "MainWindow.xaml");
        var window = XDocument.Load(windowPath);

        Assert.Equal(
            "Assets/MoniHop.Window.ico",
            (string?)window.Root?.Attribute("Icon"));
    }

    [Fact]
    public void WindowIcon_ContainsTitleBarSizes()
    {
        var path = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "MoniHop.Desktop",
            "Assets",
            "MoniHop.Window.ico");

        Assert.True(File.Exists(path), $"Window icon not found: {path}");

        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        Assert.Equal(0, reader.ReadUInt16());
        Assert.Equal(1, reader.ReadUInt16());

        var imageCount = reader.ReadUInt16();
        var sizes = new HashSet<int>();
        for (var index = 0; index < imageCount; index++)
        {
            var width = reader.ReadByte();
            var height = reader.ReadByte();
            reader.ReadBytes(14);

            var normalizedWidth = width == 0 ? 256 : width;
            var normalizedHeight = height == 0 ? 256 : height;
            Assert.Equal(normalizedWidth, normalizedHeight);
            sizes.Add(normalizedWidth);
        }

        Assert.Subset(sizes, new HashSet<int> { 16, 20, 24, 32 });
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
