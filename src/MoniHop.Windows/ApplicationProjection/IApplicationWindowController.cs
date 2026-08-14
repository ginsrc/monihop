using MoniHop.Core.ApplicationProjection;

namespace MoniHop.Windows.ApplicationProjection;

public interface IApplicationWindowController
{
    ApplicationWindowSnapshot? Read(nint windowHandle);

    IReadOnlyList<ApplicationWindowSnapshot> ReadAll();

    IReadOnlyList<ApplicationWindowSnapshot> ReadAllWindows() => ReadAll();

    void Move(nint windowHandle, ApplicationProjectionPlan plan);
}
