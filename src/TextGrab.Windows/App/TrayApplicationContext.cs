using System.Diagnostics;
using Microsoft.Win32;
using TextGrab.Configuration;
using TextGrab.Ocr;
using TextGrab.Pipeline;
using TextGrab.Platform;
using TextGrab.Windows.Platform;

namespace TextGrab.Windows.App;

/// <summary>Owns the tray icon, hotkey and the pipeline. There is no main window.</summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValue = "TextGrab";

    private readonly NotifyIcon _tray;
    private readonly Win32HotkeyService _hotkeys;
    private readonly OverlayRegionSelector _selector;
    private readonly IOcrEngine _ocr;
    private readonly GrabPipeline _pipeline;
    private readonly TextGrabSettings _settings;
    private readonly HotkeyGesture _gesture;

    public TrayApplicationContext(TextGrabSettings settings, OcrEngineRegistry engines)
    {
        _settings = settings;
        var ui = SynchronizationContext.Current ?? throw new InvalidOperationException("Must be created on the UI thread.");

        _ocr = engines.Create(settings.OcrEngine);
        _ = Task.Run(() => _ocr.WarmUpAsync());

        _selector = new OverlayRegionSelector(ui);
        var notifier = new FlashNotifier(ui, settings.ShowConfirmation);

        _pipeline = new GrabPipeline(
            new GdiScreenCapturer(),
            _selector,
            new UpscalePreprocessor(settings),
            _ocr,
            new DefaultTextPostProcessor(settings),
            new WindowsClipboard(ui),
            notifier);

        _gesture = HotkeyGesture.Parse(settings.Hotkey);
        _hotkeys = new Win32HotkeyService();
        try
        {
            _hotkeys.Register(_gesture, () => _ = Grab());
        }
        catch (InvalidOperationException ex)
        {
            notifier.Error(ex.Message);
        }

        _tray = new NotifyIcon
        {
            Icon = TrayIconFactory.Create(),
            Text = $"TextGrab  ({_gesture})  -  engine: {_ocr.Name}",
            Visible = true,
            ContextMenuStrip = BuildMenu(),
        };
        _tray.DoubleClick += (_, _) => _ = Grab();
    }

    private async Task Grab()
    {
        var outcome = await _pipeline.RunAsync();
        if (outcome is { Copied: true })
            Debug.WriteLine($"TextGrab: total {outcome.Total.TotalMilliseconds:F0} ms, ocr {outcome.Ocr.TotalMilliseconds:F0} ms, {outcome.Text.Length} chars");
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        var grab = new ToolStripMenuItem("Grab text", null, (_, _) => _ = Grab())
        {
            ShortcutKeyDisplayString = _gesture.ToString(),
            Font = new Font(menu.Font, FontStyle.Bold),
        };
        menu.Items.Add(grab);
        menu.Items.Add(new ToolStripSeparator());

        var engineItem = new ToolStripMenuItem($"Engine: {_ocr.Name}") { Enabled = false };
        menu.Items.Add(engineItem);

        menu.Items.Add("Open settings file", null, (_, _) =>
        {
            _settings.Save();
            Process.Start(new ProcessStartInfo(TextGrabSettings.DefaultPath) { UseShellExecute = true });
        });

        var startup = new ToolStripMenuItem("Start with Windows") { CheckOnClick = true, Checked = IsStartupEnabled() };
        startup.CheckedChanged += (_, _) => SetStartup(startup.Checked);
        menu.Items.Add(startup);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());
        return menu;
    }

    private static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(RunValue) is string;
    }

    private static void SetStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled) key.SetValue(RunValue, $"\"{Environment.ProcessPath}\"");
        else key.DeleteValue(RunValue, throwOnMissingValue: false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tray.Visible = false;
            _tray.Dispose();
            _hotkeys.Dispose();
            _selector.Dispose();
            _ocr.Dispose();
        }
        base.Dispose(disposing);
    }
}
