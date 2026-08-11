using MoniHop.Windows.HotKeys;

namespace MoniHop.Windows.Tests.HotKeys;

public sealed class GlobalHotKeyRegistrationTests
{
    [Fact]
    public void Register_RejectsMissingWindowHandle()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => GlobalHotKeyRegistration.Register(0));

        Assert.Equal("windowHandle", exception.ParamName);
    }
}
