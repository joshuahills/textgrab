using TextGrab.Imaging;

namespace TextGrab.Platform;

/// <summary>
/// Shows the frozen screenshot and lets the user drag a rectangle.
/// Returns the selection in the screenshot's pixel coordinates, or null if cancelled.
/// </summary>
public interface IRegionSelector
{
    Task<PixelRect?> SelectAsync(ScreenCapture capture, CancellationToken cancellationToken = default);
}
