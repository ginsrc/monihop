namespace MoniHop.Core.Displays;

public readonly record struct PixelRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;

    public int Height => Bottom - Top;

    public bool Contains(PixelPoint point) =>
        point.X >= Left && point.X < Right &&
        point.Y >= Top && point.Y < Bottom;

    public bool Contains(PixelRect other) =>
        other.Left >= Left &&
        other.Top >= Top &&
        other.Right <= Right &&
        other.Bottom <= Bottom;
}
