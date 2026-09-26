using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using TextGrab.Configuration;
using TextGrab.Imaging;
using TextGrab.Ocr.Windows;
using TextGrab.Pipeline;
using Xunit;

namespace TextGrab.Tests;

public class WindowsOcrEngineTests
{
    private static RawImage Render(string text, float fontSize, int width, int height)
    {
        using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.White);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            using var font = new Font("Segoe UI", fontSize);
            g.DrawString(text, font, Brushes.Black, 8, 8);
        }
        var data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var buf = new byte[data.Stride * height];
            Marshal.Copy(data.Scan0, buf, 0, buf.Length);
            return new RawImage(buf, width, height, data.Stride);
        }
        finally { bmp.UnlockBits(data); }
    }

    [SkippableFact]
    public async Task Recognises_RenderedSentence()
    {
        Skip.IfNot(WindowsOcrEngine.IsSupported, "No OCR language installed.");
        using var engine = WindowsOcrEngine.Create();
        await engine.WarmUpAsync();

        var img = Render("The quick brown fox jumps over 13 lazy dogs.", 20f, 700, 60);
        var result = await engine.RecognizeAsync(img);
        var text = new DefaultTextPostProcessor(new TextGrabSettings()).Format(result);

        Assert.Equal("The quick brown fox jumps over 13 lazy dogs.", text);
    }

    [SkippableFact]
    public async Task Recognises_SmallText_WhenUpscaled()
    {
        Skip.IfNot(WindowsOcrEngine.IsSupported, "No OCR language installed.");
        using var engine = WindowsOcrEngine.Create();
        var settings = new TextGrabSettings();

        var img = Render("Subtitle line one", 9f, 160, 26);
        var prepared = new UpscalePreprocessor(settings).Prepare(img);
        Assert.True(prepared.Height > img.Height);

        var result = await engine.RecognizeAsync(prepared);
        var text = new DefaultTextPostProcessor(settings).Format(result);
        Assert.Equal("Subtitle line one", text);
    }

    [SkippableFact]
    public async Task MultiLine_KeepsReadingOrder()
    {
        Skip.IfNot(WindowsOcrEngine.IsSupported, "No OCR language installed.");
        using var engine = WindowsOcrEngine.Create();
        var img = Render("First line\nSecond line\nThird line", 18f, 400, 120);
        var result = await engine.RecognizeAsync(img);
        var lines = result.Lines.Select(l => l.Text).ToArray();
        Assert.Equal(["First line", "Second line", "Third line"], lines);
    }
}
