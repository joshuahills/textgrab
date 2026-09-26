using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using TextGrab.Imaging;

namespace TextGrab.Windows.Platform;

/// <summary>Wraps a <see cref="RawImage"/> as a GDI+ Bitmap without copying pixels.</summary>
internal sealed class PinnedBitmap : IDisposable
{
    private GCHandle _handle;
    public Bitmap Bitmap { get; }

    public PinnedBitmap(RawImage image)
    {
        _handle = GCHandle.Alloc(image.Pixels, GCHandleType.Pinned);
        Bitmap = new Bitmap(image.Width, image.Height, image.Stride, PixelFormat.Format32bppArgb, _handle.AddrOfPinnedObject());
    }

    public void Dispose()
    {
        Bitmap.Dispose();
        if (_handle.IsAllocated) _handle.Free();
    }
}
