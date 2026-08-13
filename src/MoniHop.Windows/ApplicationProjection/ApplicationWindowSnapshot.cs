using MoniHop.Core.ApplicationProjection;
using MoniHop.Windows.Windows;

namespace MoniHop.Windows.ApplicationProjection;

public sealed record ApplicationWindowSnapshot(
    nint WindowHandle,
    ApplicationIdentity Application,
    string DisplayName,
    WindowPlacementSnapshot Placement);
