using System.Diagnostics;
using TextGrab.Imaging;
using TextGrab.Ocr;
using TextGrab.Platform;

namespace TextGrab.Pipeline;

public sealed record GrabOutcome(bool Copied, string Text, PixelRect? Selection, TimeSpan Total, TimeSpan Ocr);

/// <summary>
/// The whole capture -> select -> crop -> OCR -> clipboard flow, independent of platform.
/// Hosts construct this with their platform services and call <see cref="RunAsync"/> from the hotkey.
/// </summary>
public sealed class GrabPipeline(
    IScreenCapturer capturer,
    IRegionSelector selector,
    IImagePreprocessor preprocessor,
    IOcrEngine ocr,
    ITextPostProcessor postProcessor,
    IClipboard clipboard,
    INotifier notifier)
{
    private int _running;

    public bool IsRunning => Volatile.Read(ref _running) == 1;

    public async Task<GrabOutcome?> RunAsync(CancellationToken cancellationToken = default)
    {
        // Ignore re-entrant hotkey presses while an overlay is already up.
        if (Interlocked.Exchange(ref _running, 1) == 1) return null;
        var sw = Stopwatch.StartNew();
        try
        {
            var capture = capturer.CaptureAll();
            var selection = await selector.SelectAsync(capture, cancellationToken).ConfigureAwait(false);
            if (selection is not { IsEmpty: false } rect)
                return new GrabOutcome(false, string.Empty, null, sw.Elapsed, TimeSpan.Zero);

            var cropped = preprocessor.Prepare(capture.Image.Crop(rect));
            var result = await ocr.RecognizeAsync(cropped, cancellationToken).ConfigureAwait(false);
            var text = postProcessor.Format(result);

            var screenRect = rect.Offset(capture.VirtualBounds.X, capture.VirtualBounds.Y);
            if (text.Length == 0)
            {
                notifier.Error("No text found");
                return new GrabOutcome(false, text, screenRect, sw.Elapsed, result.Elapsed);
            }

            await clipboard.SetTextAsync(text, cancellationToken).ConfigureAwait(false);
            notifier.Success(Summarise(text), screenRect);
            return new GrabOutcome(true, text, screenRect, sw.Elapsed, result.Elapsed);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            notifier.Error(ex.Message);
            return null;
        }
        finally
        {
            Volatile.Write(ref _running, 0);
        }
    }

    private static string Summarise(string text)
    {
        var flat = text.ReplaceLineEndings(" ");
        return flat.Length <= 60 ? flat : flat[..57] + "...";
    }
}
