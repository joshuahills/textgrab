using TextGrab.Imaging;

namespace TextGrab.Platform;

/// <summary>Lightweight user feedback (a brief "Copied" flash, or an error). Must never block.</summary>
public interface INotifier
{
    void Success(string message, PixelRect? nearScreenRect = null);
    void Error(string message);
}
