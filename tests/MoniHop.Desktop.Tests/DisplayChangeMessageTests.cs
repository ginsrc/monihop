using MoniHop.Desktop;

namespace MoniHop.Desktop.Tests;

public sealed class DisplayChangeMessageTests
{
    [Theory]
    [InlineData(0x007E)]
    [InlineData(0x001B)]
    public void RequiresRefresh_ReturnsTrueForDisplayRelatedMessages(int message)
    {
        Assert.True(DisplayChangeMessage.RequiresRefresh(message));
    }

    [Theory]
    [InlineData(0x000F)]
    [InlineData(0x0312)]
    [InlineData(0x001A)]
    [InlineData(0x0219)]
    [InlineData(0x02E0)]
    public void RequiresRefresh_ReturnsFalseForUnrelatedMessages(int message)
    {
        Assert.False(DisplayChangeMessage.RequiresRefresh(message));
    }

    [Fact]
    public void RequiresThemeRefresh_ReturnsTrueOnlyForSettingChange()
    {
        Assert.True(DisplayChangeMessage.RequiresThemeRefresh(0x001A));
        Assert.False(DisplayChangeMessage.RequiresThemeRefresh(0x007E));
    }
}
