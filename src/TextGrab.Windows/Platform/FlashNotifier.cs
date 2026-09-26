using TextGrab.Imaging;
using TextGrab.Platform;
using TextGrab.Windows.Interop;

namespace TextGrab.Windows.Platform;

/// <summary>A tiny click-through toast that appears next to the selection for a moment and fades out.</summary>
public sealed class FlashNotifier(SynchronizationContext ui, bool enabled) : INotifier
{
    public void Success(string message, PixelRect? nearScreenRect = null)
    {
        if (!enabled) return;
        Show("Copied  " + message, Color.FromArgb(32, 32, 32), nearScreenRect);
    }

    public void Error(string message) => Show(message, Color.FromArgb(140, 30, 30), null);

    private void Show(string text, Color back, PixelRect? near)
        => ui.Post(_ => new FlashForm(text, back, near).Show(), null);

    private sealed class FlashForm : Form
    {
        private readonly System.Windows.Forms.Timer _timer = new() { Interval = 30 };
        private int _ticks;
        private const int HoldTicks = 40;   // ~1.2 s fully visible
        private const int FadeTicks = 12;   // ~0.36 s fade

        public FlashForm(string text, Color back, PixelRect? near)
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = back;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10f);
            Opacity = 0.96;

            var label = new Label
            {
                AutoSize = true,
                Text = text,
                Padding = new Padding(12, 8, 12, 8),
                MaximumSize = new Size(520, 0),
            };
            Controls.Add(label);
            ClientSize = label.PreferredSize;

            var anchor = near is { } r
                ? new Point(r.X, r.Bottom + 8)
                : new Point(Screen.PrimaryScreen!.WorkingArea.Right - Width - 16, Screen.PrimaryScreen.WorkingArea.Bottom - Height - 16);
            var screen = Screen.FromPoint(anchor).WorkingArea;
            anchor.X = Math.Clamp(anchor.X, screen.Left, Math.Max(screen.Left, screen.Right - Width));
            if (anchor.Y + Height > screen.Bottom && near is { } nr) anchor.Y = nr.Y - Height - 8;
            anchor.Y = Math.Clamp(anchor.Y, screen.Top, Math.Max(screen.Top, screen.Bottom - Height));
            Location = anchor;

            _timer.Tick += (_, _) =>
            {
                _ticks++;
                if (_ticks <= HoldTicks) return;
                var fade = _ticks - HoldTicks;
                if (fade >= FadeTicks) { _timer.Stop(); Close(); return; }
                Opacity = 0.96 * (1 - fade / (double)FadeTicks);
            };
            _timer.Start();
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE | Native.WS_EX_TRANSPARENT | Native.WS_EX_TOPMOST;
                return cp;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer.Dispose();
            base.Dispose(disposing);
        }
    }
}
