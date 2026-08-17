using MoniHop.Desktop.Settings;

namespace MoniHop.Desktop.Tests.Settings;

public sealed class MoniHopPathsTests
{
    [Fact]
    public void CreateDefault_UsesCurrentWindowsLocalApplicationData()
    {
        var localApplicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);

        var paths = MoniHopPaths.CreateDefault();

        Assert.Equal(Path.Combine(localApplicationData, "MoniHop"), paths.DataDirectory);
        Assert.Equal(Path.Combine(paths.DataDirectory, "config"), paths.ConfigurationDirectory);
        Assert.Equal(Path.Combine(paths.DataDirectory, "diagnostics"), paths.DiagnosticsDirectory);
        Assert.False(paths.IsPortable);
    }

    [Fact]
    public void CreateForExecutableDirectory_WithPortableMarker_UsesLocalDataDirectory()
    {
        var executableDirectory = Directory.CreateTempSubdirectory("monihop-portable-");
        try
        {
            File.WriteAllText(
                Path.Combine(executableDirectory.FullName, MoniHopPaths.PortableMarkerFileName),
                string.Empty);

            var paths = MoniHopPaths.CreateForExecutableDirectory(executableDirectory.FullName);

            Assert.True(paths.IsPortable);
            Assert.Equal(Path.Combine(executableDirectory.FullName, "data"), paths.DataDirectory);
            Assert.Equal(Path.Combine(paths.DataDirectory, "config"), paths.ConfigurationDirectory);
            Assert.Equal(Path.Combine(paths.DataDirectory, "diagnostics"), paths.DiagnosticsDirectory);
        }
        finally
        {
            executableDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public void CreateForExecutableDirectory_WithoutPortableMarker_UsesLocalApplicationData()
    {
        var executableDirectory = Directory.CreateTempSubdirectory("monihop-installed-");
        try
        {
            var paths = MoniHopPaths.CreateForExecutableDirectory(executableDirectory.FullName);

            Assert.False(paths.IsPortable);
            Assert.Equal(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MoniHop"),
                paths.DataDirectory);
        }
        finally
        {
            executableDirectory.Delete(recursive: true);
        }
    }
}
