using TextGrab.Imaging;

namespace TextGrab.Ocr;

/// <summary>
/// A text recogniser. Implementations must be safe to keep alive for the lifetime of the app
/// and should do any expensive initialisation in <see cref="WarmUpAsync"/> so the first real
/// grab is as fast as the tenth.
/// </summary>
public interface IOcrEngine : IDisposable
{
    /// <summary>Stable identifier used in settings, e.g. "windows", "tesseract", "onnx-small".</summary>
    string Name { get; }

    /// <summary>Preload models / native engines. Called once at startup off the UI thread.</summary>
    Task WarmUpAsync(CancellationToken cancellationToken = default);

    Task<OcrResult> RecognizeAsync(RawImage image, CancellationToken cancellationToken = default);
}
