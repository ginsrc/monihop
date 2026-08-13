using MoniHop.Core.ApplicationProjection;
using MoniHop.Desktop.Models;
using ProjectionApplicationIdentity = MoniHop.Core.ApplicationProjection.ApplicationIdentity;

namespace MoniHop.Desktop.Tests.Models;

public sealed class ApplicationRuleViewModelTests
{
    [Theory]
    [InlineData(ProjectionLayout.KeepSize, "保持尺寸")]
    [InlineData(ProjectionLayout.Maximized, "最大化")]
    [InlineData(ProjectionLayout.LeftHalf, "左半屏")]
    [InlineData(ProjectionLayout.RightHalf, "右半屏")]
    public void Create_FormatsLayout(ProjectionLayout layout, string expected)
    {
        var rule = Rule(layout, true);

        var model = ApplicationRuleViewModel.Create(rule, "工作屏", true);

        Assert.Equal(expected, model.LayoutName);
        Assert.Equal("已启用", model.State);
        Assert.Equal("工作屏", model.TargetDisplayName);
    }

    [Fact]
    public void Create_MarksMissingTargetWithoutHidingPausedState()
    {
        var model = ApplicationRuleViewModel.Create(Rule(ProjectionLayout.KeepSize, false), null, false);

        Assert.Equal("目标不可用", model.TargetDisplayName);
        Assert.Equal("已暂停 · 目标不可用", model.State);
    }

    private static ApplicationProjectionRule Rule(ProjectionLayout layout, bool isEnabled) =>
        new(
            new ProjectionApplicationIdentity(ApplicationIdentityKind.ExecutablePath, @"C:\Apps\browser.exe"),
            "Browser",
            "stable-a",
            layout,
            isEnabled);
}
