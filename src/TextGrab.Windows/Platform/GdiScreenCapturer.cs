using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using TextGrab.Imaging;
using TextGrab.Platform;
using TextGrab.Windows.Interop;

namespace TextGrab.Windows.Platform;

/// <summary>
/// Captures the whole virtual desktop with a single BitBlt. On Windows 11 this is ~10-30 ms for a 4K desktop,
/// which is fast enough for the hotkey path and needs no Direct3D setup.
/// </summary>
public sealed class GdiScreenCapturer : IScreenCapturer
{
    public ScreenCapture CaptureAll()
    {
        var vs = SystemInformation.VirtualScreen;
        var bounds = new PixelRect(vs.X, vs.Y, vs.Width, vs.Height);

        using var bmp = new Bitmap(vs.Width, vs.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            var hdcDest = g.GetHdc();
            var hdcSrc = Native.GetDC(nint.Zero);
            try
            {
                Native.BitBlt(hdcDest, 0, 0, vs.Width, vs.Height, hdcSrc, vs.X, vs.Y, Native.SRCCOPY | Native.CAPTUREBLT);
            }
            finally
            {
                Native.ReleaseDC(nint.Zero, hdcSrc);
                g.ReleaseHdc(hdcDest);
            }
        }

        return new ScreenCapture(ToRawImage(bmp), bounds);
    }

    private static RawImage ToRawImage(Bitmap bmp)
    {
        var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var stride = Math.Abs(data.Stride);
            var buffer = new byte[stride * bmp.Height];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
            return new RawImage(buffer, bmp.Width, bmp.Height, stride);
        }
        finally
        {
            bmp.UnlockBits(data);
        }
    }
}
