using TextGrab.Configuration;
using TextGrab.Ocr;
using TextGrab.Ocr.Windows;
using TextGrab.Windows.App;

namespace TextGrab.Windows;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        using var singleInstance = new Mutex(initiallyOwned: true, @"Local\TextGrab.SingleInstance", out var isFirst);
        if (!isFirst) return 0;

        ApplicationConfiguration.Initialize();

        TextGrabSettings settings;
        try
        {
            settings = TextGrabSettings.Load();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not read settings:\n{ex.Message}\n\nUsing defaults.", "TextGrab", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            settings = new TextGrabSettings();
        }

        // Add new backends here (or discover them via plugins later). Order = fallback preference.
        var engines = new OcrEngineRegistry()
            .Register(new WindowsOcrEngineFactory());

        try
        {
            using var context = new TrayApplicationContext(settings, engines);
            Application.Run(context);
            return 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "TextGrab failed to start", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }
}
