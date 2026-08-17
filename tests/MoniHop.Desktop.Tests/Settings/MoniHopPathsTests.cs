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
    }
}
