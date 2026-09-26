using TextGrab.Imaging;

namespace TextGrab.Platform;

public sealed record ScreenCapture(RawImage Image, PixelRect VirtualBounds);

/// <summary>Grabs every monitor into one image. Must be fast; it runs on the hotkey critical path.</summary>
public interface IScreenCapturer
{
    ScreenCapture CaptureAll();
}
