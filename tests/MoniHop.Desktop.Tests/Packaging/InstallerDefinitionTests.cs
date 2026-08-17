namespace MoniHop.Desktop.Tests.Packaging;

public sealed class InstallerDefinitionTests
{
    [Fact]
    public void Uninstall_RemovesStartupIntegrationAndAllApplicationData()
    {
        var script = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "packaging",
            "MoniHop.iss"));

        Assert.Contains(
            "Root: HKCU; Subkey: \"Software\\Microsoft\\Windows\\CurrentVersion\\Run\"; " +
            "ValueType: none; ValueName: \"MoniHop\"; Flags: uninsdeletevalue",
            script,
            StringComparison.Ordinal);
        Assert.Contains(
            "Parameters: \"/Delete /F /TN \"\"MoniHop Startup\"\"\"",
            script,
            StringComparison.Ordinal);
        Assert.Contains(
            "Type: filesandordirs; Name: \"{localappdata}\\MoniHop\"",
            script,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Installer_UsesProductExecutableAndCurrentUserDirectory()
    {
        var script = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "packaging",
            "MoniHop.iss"));

        Assert.Contains("#define AppExeName \"MoniHop.exe\"", script, StringComparison.Ordinal);
        Assert.Contains(
            "DefaultDirName={localappdata}\\Programs\\{#AppName}",
            script,
            StringComparison.Ordinal);
        Assert.Contains("PrivilegesRequired=lowest", script, StringComparison.Ordinal);
        Assert.Contains(
            "MessagesFile: \"compiler:Languages\\ChineseSimplified.isl\"",
            script,
            StringComparison.Ordinal);
        Assert.Contains(
            "MessagesFile: \"compiler:Default.isl\"",
            script,
            StringComparison.Ordinal);
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
