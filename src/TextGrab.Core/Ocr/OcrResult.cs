using TextGrab.Imaging;

namespace TextGrab.Ocr;

public sealed record OcrWord(string Text, PixelRect Bounds);

public sealed record OcrLine(string Text, PixelRect Bounds, IReadOnlyList<OcrWord> Words);

/// <summary>Engine-agnostic recognition output. Bounds are relative to the image that was recognised.</summary>
public sealed record OcrResult(IReadOnlyList<OcrLine> Lines, TimeSpan Elapsed, string EngineName)
{
    public static OcrResult Empty(string engine) => new([], TimeSpan.Zero, engine);
    public bool HasText => Lines.Any(l => !string.IsNullOrWhiteSpace(l.Text));
}
