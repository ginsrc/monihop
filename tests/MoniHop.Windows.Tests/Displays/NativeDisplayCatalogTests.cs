using MoniHop.Windows.Displays;

namespace MoniHop.Windows.Tests.Displays;

public sealed class NativeDisplayCatalogTests
{
    [Fact]
    public void ReadAll_ReturnsAtLeastOneDisplayAndExactlyOnePrimaryDisplay()
    {
        var catalog = new NativeDisplayCatalog();

        var displays = catalog.ReadAll();

        Assert.NotEmpty(displays);
        Assert.Single(displays, display => display.IsPrimary);
        Assert.All(displays, display => Assert.False(string.IsNullOrWhiteSpace(display.DeviceName)));
    }
}
