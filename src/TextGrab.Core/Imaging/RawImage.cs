namespace TextGrab.Imaging;

/// <summary>
/// A platform-neutral 32-bit BGRA bitmap. Every platform layer converts to and from this type,
/// so the pipeline and OCR engines never depend on System.Drawing, WinRT, Skia, etc.
/// </summary>
public sealed class RawImage
{
    public const int BytesPerPixel = 4;

    public byte[] Pixels { get; }
    public int Width { get; }
    public int Height { get; }
    /// <summary>Bytes per row. May be larger than Width * 4 when rows are padded.</summary>
    public int Stride { get; }

    public RawImage(byte[] pixels, int width, int height, int stride)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Image must be non-empty.");
        if (stride < width * BytesPerPixel) throw new ArgumentOutOfRangeException(nameof(stride));
        if (pixels.Length < stride * height) throw new ArgumentException("Pixel buffer too small.", nameof(pixels));
        Pixels = pixels;
        Width = width;
        Height = height;
        Stride = stride;
    }

    public static RawImage Allocate(int width, int height)
        => new(new byte[width * BytesPerPixel * height], width, height, width * BytesPerPixel);

    public bool IsTightlyPacked => Stride == Width * BytesPerPixel;

    /// <summary>Returns a tightly packed copy of the pixels (or the original buffer if already packed).</summary>
    public byte[] ToPackedBytes()
    {
        if (IsTightlyPacked && Pixels.Length == Stride * Height) return Pixels;
        var rowBytes = Width * BytesPerPixel;
        var packed = new byte[rowBytes * Height];
        for (var y = 0; y < Height; y++)
            Buffer.BlockCopy(Pixels, y * Stride, packed, y * rowBytes, rowBytes);
        return packed;
    }

    public RawImage Crop(PixelRect rect)
    {
        rect = rect.Intersect(new PixelRect(0, 0, Width, Height));
        if (rect.IsEmpty) throw new ArgumentException("Crop rectangle does not intersect the image.", nameof(rect));

        var result = Allocate(rect.Width, rect.Height);
        var rowBytes = rect.Width * BytesPerPixel;
        for (var y = 0; y < rect.Height; y++)
        {
            var src = (rect.Y + y) * Stride + rect.X * BytesPerPixel;
            Buffer.BlockCopy(Pixels, src, result.Pixels, y * result.Stride, rowBytes);
        }
        return result;
    }

    /// <summary>Bilinear resize. Good enough for OCR upscaling and keeps the core free of native deps.</summary>
    public RawImage Resize(int newWidth, int newHeight)
    {
        if (newWidth == Width && newHeight == Height) return this;
        var dst = Allocate(newWidth, newHeight);
        var xRatio = (Width - 1) / (double)Math.Max(1, newWidth - 1);
        var yRatio = (Height - 1) / (double)Math.Max(1, newHeight - 1);

        for (var y = 0; y < newHeight; y++)
        {
            var sy = y * yRatio;
            var y0 = (int)sy;
            var y1 = Math.Min(y0 + 1, Height - 1);
            var fy = sy - y0;
            for (var x = 0; x < newWidth; x++)
            {
                var sx = x * xRatio;
                var x0 = (int)sx;
                var x1 = Math.Min(x0 + 1, Width - 1);
                var fx = sx - x0;

                var p00 = y0 * Stride + x0 * BytesPerPixel;
                var p10 = y0 * Stride + x1 * BytesPerPixel;
                var p01 = y1 * Stride + x0 * BytesPerPixel;
                var p11 = y1 * Stride + x1 * BytesPerPixel;
                var d = y * dst.Stride + x * BytesPerPixel;

                for (var c = 0; c < BytesPerPixel; c++)
                {
                    var top = Pixels[p00 + c] + (Pixels[p10 + c] - Pixels[p00 + c]) * fx;
                    var bottom = Pixels[p01 + c] + (Pixels[p11 + c] - Pixels[p01 + c]) * fx;
                    dst.Pixels[d + c] = (byte)Math.Clamp(top + (bottom - top) * fy + 0.5, 0, 255);
                }
            }
        }
        return dst;
    }
}
