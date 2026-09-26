using TextGrab.Configuration;
using TextGrab.Imaging;

namespace TextGrab.Pipeline;

/// <summary>Prepares a cropped selection for OCR. Kept separate so engines can opt in/out or add their own steps.</summary>
public interface IImagePreprocessor
{
    RawImage Prepare(RawImage image);
}

public sealed class UpscalePreprocessor(TextGrabSettings settings) : IImagePreprocessor
{
    public RawImage Prepare(RawImage image)
    {
        if (image.Height >= settings.MinOcrHeight) return image;
        var factor = Math.Min(settings.MaxUpscale, settings.MinOcrHeight / (double)image.Height);
        if (factor <= 1.05) return image;
        return image.Resize((int)Math.Round(image.Width * factor), (int)Math.Round(image.Height * factor));
    }
}
