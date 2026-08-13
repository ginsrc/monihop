namespace MoniHop.Windows.ApplicationProjection;

public readonly record struct WindowEvent(WindowEventKind Kind, nint WindowHandle)
{
    public static bool IsWindowEvent(WindowEventKind kind, int objectId, int childId) =>
        (kind is WindowEventKind.Shown or WindowEventKind.Destroyed) &&
        objectId == 0 &&
        childId == 0;
}

public enum WindowEventKind
{
    Shown,
    Destroyed,
}
