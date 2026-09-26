namespace TextGrab.Imaging;

/// <summary>An integer rectangle in pixel coordinates.</summary>
public readonly record struct PixelRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;
    public bool IsEmpty => Width <= 0 || Height <= 0;

    public static PixelRect FromCorners(int x1, int y1, int x2, int y2)
        => new(Math.Min(x1, x2), Math.Min(y1, y2), Math.Abs(x2 - x1), Math.Abs(y2 - y1));

    public PixelRect Intersect(PixelRect other)
    {
        var x1 = Math.Max(X, other.X);
        var y1 = Math.Max(Y, other.Y);
        var x2 = Math.Min(Right, other.Right);
        var y2 = Math.Min(Bottom, other.Bottom);
        return x2 <= x1 || y2 <= y1 ? default : new PixelRect(x1, y1, x2 - x1, y2 - y1);
    }

    public PixelRect Offset(int dx, int dy) => new(X + dx, Y + dy, Width, Height);
}
