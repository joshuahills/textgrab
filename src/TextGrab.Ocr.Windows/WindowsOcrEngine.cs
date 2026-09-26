using System.Diagnostics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using TextGrab.Imaging;
using TextGrab.Ocr;
using WinOcrEngine = Windows.Media.Ocr.OcrEngine;

namespace TextGrab.Ocr.Windows;

/// <summary>
/// Backend using the OCR engine that ships with Windows 10/11 (the same one PowerToys uses).
/// Zero download, fast, and good at rendered screen text.
/// </summary>
public sealed class WindowsOcrEngine : IOcrEngine
{
    public const string EngineName = "windows";

    private readonly WinOcrEngine _engine;

    public string Name => EngineName;

    private WindowsOcrEngine(WinOcrEngine engine) => _engine = engine;

    public static bool IsSupported => WinOcrEngine.AvailableRecognizerLanguages.Count > 0;

    public static WindowsOcrEngine Create(string? languageTag = null)
    {
        WinOcrEngine? engine = null;
        if (languageTag is not null && Language.IsWellFormed(languageTag))
            engine = WinOcrEngine.TryCreateFromLanguage(new Language(languageTag));
        engine ??= WinOcrEngine.TryCreateFromUserProfileLanguages();
        if (engine is null)
            throw new InvalidOperationException(
                "Windows OCR is not available. Install a language pack with OCR support in Settings > Time & Language.");
        return new WindowsOcrEngine(engine);
    }

    public async Task WarmUpAsync(CancellationToken cancellationToken = default)
    {
        // Run a tiny recognition so the native engine and language model are paged in.
        var blank = RawImage.Allocate(64, 32);
        Array.Fill(blank.Pixels, (byte)255);
        await RecognizeAsync(blank, cancellationToken).ConfigureAwait(false);
    }

    public async Task<OcrResult> RecognizeAsync(RawImage image, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        image = FitToEngineLimits(image);

        using var bitmap = SoftwareBitmap.CreateCopyFromBuffer(
            image.ToPackedBytes().AsBuffer(),
            BitmapPixelFormat.Bgra8,
            image.Width,
            image.Height,
            BitmapAlphaMode.Premultiplied);

        var native = await _engine.RecognizeAsync(bitmap).AsTask(cancellationToken).ConfigureAwait(false);

        var lines = new List<OcrLine>(native.Lines.Count);
        foreach (var line in native.Lines)
        {
            var words = line.Words.Select(w => new OcrWord(w.Text, ToPixelRect(w.BoundingRect))).ToList();
            var bounds = words.Count == 0 ? default : Union(words.Select(w => w.Bounds));
            lines.Add(new OcrLine(line.Text, bounds, words));
        }

        // The engine may return lines out of reading order for multi-column or rotated text; sort top-to-bottom.
        lines.Sort((a, b) => a.Bounds.Y != b.Bounds.Y ? a.Bounds.Y.CompareTo(b.Bounds.Y) : a.Bounds.X.CompareTo(b.Bounds.X));
        return new OcrResult(lines, sw.Elapsed, Name);
    }

    private static RawImage FitToEngineLimits(RawImage image)
    {
        var max = (int)WinOcrEngine.MaxImageDimension;
        var largest = Math.Max(image.Width, image.Height);
        if (largest <= max) return image;
        var scale = max / (double)largest;
        return image.Resize(Math.Max(1, (int)(image.Width * scale)), Math.Max(1, (int)(image.Height * scale)));
    }

    private static PixelRect ToPixelRect(global::Windows.Foundation.Rect r)
        => new((int)Math.Floor(r.X), (int)Math.Floor(r.Y), (int)Math.Ceiling(r.Width), (int)Math.Ceiling(r.Height));

    private static PixelRect Union(IEnumerable<PixelRect> rects)
    {
        int x1 = int.MaxValue, y1 = int.MaxValue, x2 = int.MinValue, y2 = int.MinValue;
        foreach (var r in rects)
        {
            x1 = Math.Min(x1, r.X); y1 = Math.Min(y1, r.Y);
            x2 = Math.Max(x2, r.Right); y2 = Math.Max(y2, r.Bottom);
        }
        return x1 == int.MaxValue ? default : new PixelRect(x1, y1, x2 - x1, y2 - y1);
    }

    public void Dispose() { }
}

public sealed class WindowsOcrEngineFactory : IOcrEngineFactory
{
    public string Name => WindowsOcrEngine.EngineName;
    public string DisplayName => "Windows built-in OCR";
    public bool IsAvailable => OperatingSystem.IsWindowsVersionAtLeast(10) && WindowsOcrEngine.IsSupported;
    public IOcrEngine Create() => WindowsOcrEngine.Create();
}
