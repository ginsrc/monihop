using MoniHop.Core.ApplicationProjection;
using MoniHop.Windows.Windows;

namespace MoniHop.Windows.ApplicationProjection;

public sealed record ApplicationWindowSnapshot
{
    public ApplicationWindowSnapshot(
        nint windowHandle,
        ApplicationIdentity application,
        string displayName,
        WindowPlacementSnapshot placement)
        : this(windowHandle, application, displayName, placement, WindowCapabilities.Standard)
    {
    }

    public ApplicationWindowSnapshot(
        nint windowHandle,
        ApplicationIdentity application,
        string displayName,
        WindowPlacementSnapshot placement,
        WindowCapabilities capabilities)
    {
        WindowHandle = windowHandle;
        Application = application;
        DisplayName = displayName;
        Placement = placement;
        Capabilities = capabilities;
    }

    public nint WindowHandle { get; init; }

    public ApplicationIdentity Application { get; init; }

    public string DisplayName { get; init; }

    public WindowPlacementSnapshot Placement { get; init; }

    public WindowCapabilities Capabilities { get; init; }
}
