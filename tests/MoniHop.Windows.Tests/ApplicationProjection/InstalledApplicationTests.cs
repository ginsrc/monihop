using MoniHop.Core.ApplicationProjection;
using MoniHop.Windows.ApplicationProjection;

namespace MoniHop.Windows.Tests.ApplicationProjection;

public sealed class InstalledApplicationTests
{
    [Fact]
    public void Create_UsesExecutablePathForDesktopApplication()
    {
        var application = InstalledApplication.Create(
            "Microsoft Edge",
            "MSEdge",
            @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe");

        Assert.NotNull(application);
        Assert.Equal(ApplicationIdentityKind.ExecutablePath, application.Identity.Kind);
        Assert.Equal(
            @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
            application.Identity.Value);
    }

    [Fact]
    public void Create_UsesAumidForPackagedApplication()
    {
        var application = InstalledApplication.Create(
            "计算器",
            "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App",
            null);

        Assert.NotNull(application);
        Assert.Equal(ApplicationIdentityKind.ApplicationUserModelId, application.Identity.Kind);
        Assert.Equal("Microsoft.WindowsCalculator_8wekyb3d8bbwe!App", application.Identity.Value);
    }

    [Theory]
    [InlineData("", "App!Id", null)]
    [InlineData("网页", "https://example.test", "https://example.test")]
    [InlineData("卸载程序", "uninstall", @"C:\Apps\uninstall.exe")]
    public void Create_RejectsEntriesThatAreNotLaunchableApplications(
        string name,
        string? appUserModelId,
        string? targetPath)
    {
        Assert.Null(InstalledApplication.Create(name, appUserModelId, targetPath));
    }
}
