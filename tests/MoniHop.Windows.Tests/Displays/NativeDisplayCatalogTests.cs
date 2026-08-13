using MoniHop.Core.Displays;
using MoniHop.Windows.Displays;
using System.Runtime.Versioning;

namespace MoniHop.Windows.Tests.Displays;

[SupportedOSPlatform("windows")]
public sealed class NativeDisplayCatalogTests
{
    [Fact]
    public void EdidPhysicalSizeParser_UsesStandardImageSizeFields()
    {
        var edid = new byte[128];
        edid[21] = 60;
        edid[22] = 33;

        var result = EdidPhysicalSizeParser.Parse(edid);

        Assert.Equal((600, 330), result);
    }

    [Fact]
    public void EdidPhysicalSizeParser_ReturnsUnknownWhenFieldsAreUnavailable()
    {
        Assert.Null(EdidPhysicalSizeParser.Parse(new byte[22]));

        var edid = new byte[128];
        Assert.Null(EdidPhysicalSizeParser.Parse(edid));
    }

    [Fact]
    public void ReadAll_ReturnsAtLeastOneDisplayAndExactlyOnePrimaryDisplay()
    {
        var catalog = new NativeDisplayCatalog();

        var displays = catalog.ReadAll();

        Assert.NotEmpty(displays);
        Assert.Single(displays, display => display.IsPrimary);
        Assert.All(displays, display => Assert.False(string.IsNullOrWhiteSpace(display.DeviceName)));
        Assert.All(displays, display => Assert.True(display.ResolutionWidth is > 0));
        Assert.All(displays, display => Assert.True(display.ResolutionHeight is > 0));
        Assert.All(displays, display => Assert.True(display.RefreshRateHz is > 0));
        Assert.All(displays, display => Assert.True(display.ScalePercent is >= 100));
        Assert.All(displays, display => Assert.NotEqual(DisplayOrientation.Unknown, display.Orientation));
        Assert.All(displays, display => Assert.True(display.PhysicalWidthMillimeters is > 0));
        Assert.All(displays, display => Assert.True(display.PhysicalHeightMillimeters is > 0));
    }
}
