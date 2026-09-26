using System.Drawing.Drawing2D;
using TextGrab.Imaging;
using TextGrab.Platform;
using TextGrab.Windows.Interop;

namespace TextGrab.Windows.Platform;

/// <summary>
/// Full-virtual-screen, borderless, top-most window that shows the frozen screenshot dimmed and lets the
/// user drag a rectangle. It is created once at startup and reused, so showing it is just a ShowWindow call.
/// </summary>
internal sealed class OverlayForm : Form
{
    private static readonly Brush DimBrush = new SolidBrush(Color.FromArgb(110, 0, 0, 0));
    private static readonly Pen BorderPen = new(Color.White, 1f) { DashStyle = DashStyle.Solid };
    private static readonly Pen AccentPen = new(Color.FromArgb(0, 120, 215), 1f);
    private static readonly Font LabelFont = new("Segoe UI", 9f);

    private PinnedBitmap? _screenshot;
    private PixelRect _virtualBounds;
    private Point _dragStart;
    private Rectangle _selection;
    private bool _dragging;
    private TaskCompletionSource<PixelRect?>? _completion;

    public OverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        Cursor = Cursors.Cross;
        KeyPreview = true;
        BackColor = Color.Black;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Text = "TextGrab";
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= Native.WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    /// <summary>Begins a selection session. Must be called on the UI thread.</summary>
    public Task<PixelRect?> BeginSelectAsync(ScreenCapture capture, CancellationToken cancellationToken)
    {
        if (_completion is not null) return _completion.Task;

        _completion = new TaskCompletionSource<PixelRect?>(TaskCreationOptions.RunContinuationsAsynchronously);
        cancellationToken.Register(() => Finish(null), useSynchronizationContext: true);

        _screenshot?.Dispose();
        _screenshot = new PinnedBitmap(capture.Image);
        _virtualBounds = capture.VirtualBounds;
        _selection = Rectangle.Empty;
        _dragging = false;

        Bounds = new Rectangle(_virtualBounds.X, _virtualBounds.Y, _virtualBounds.Width, _virtualBounds.Height);
        Show();
        Native.SetForegroundWindow(Handle);
        Activate();
        Invalidate();
        return _completion.Task;
    }

    private void Finish(PixelRect? result)
    {
        var tcs = _completion;
        if (tcs is null) return;
        _completion = null;
        Hide();
        _screenshot?.Dispose();
        _screenshot = null;
        tcs.TrySetResult(result);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right) { Finish(null); return; }
        if (e.Button != MouseButtons.Left) return;
        _dragging = true;
        _dragStart = e.Location;
        UpdateSelection(new Rectangle(e.Location, Size.Empty));
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!_dragging) return;
        var r = Rectangle.FromLTRB(
            Math.Min(_dragStart.X, e.X), Math.Min(_dragStart.Y, e.Y),
            Math.Max(_dragStart.X, e.X), Math.Max(_dragStart.Y, e.Y));
        UpdateSelection(r);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (!_dragging || e.Button != MouseButtons.Left) return;
        _dragging = false;
        var r = _selection;
        if (r.Width < 3 || r.Height < 3) { Finish(null); return; }
        Finish(new PixelRect(r.X, r.Y, r.Width, r.Height));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) { Finish(null); e.Handled = true; return; }
        base.OnKeyDown(e);
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        // Losing focus while up (e.g. another hotkey app stealing it) should cancel rather than leave a stuck overlay.
        if (_completion is not null && Visible) Finish(null);
    }

    private void UpdateSelection(Rectangle next)
    {
        var old = _selection;
        _selection = next;
        var dirty = Rectangle.Union(old, next);
        dirty.Inflate(2, 24); // include label and border
        Invalidate(dirty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_screenshot is null) return;
        var g = e.Graphics;
        var clip = e.ClipRectangle;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.CompositingMode = CompositingMode.SourceCopy;
        g.DrawImage(_screenshot.Bitmap, clip, clip, GraphicsUnit.Pixel);
        g.CompositingMode = CompositingMode.SourceOver;

        // Dim everything except the selection.
        if (_selection.IsEmpty)
        {
            g.FillRectangle(DimBrush, clip);
            return;
        }

        g.SetClip(_selection, CombineMode.Exclude);
        g.FillRectangle(DimBrush, clip);
        g.ResetClip();

        var border = _selection;
        border.Width -= 1; border.Height -= 1;
        g.DrawRectangle(BorderPen, border);
        border.Inflate(1, 1);
        g.DrawRectangle(AccentPen, border);

        var label = $"{_selection.Width} x {_selection.Height}";
        var size = g.MeasureString(label, LabelFont);
        var labelPos = new PointF(_selection.X, _selection.Y - size.Height - 2);
        if (labelPos.Y < 0) labelPos.Y = _selection.Bottom + 2;
        g.FillRectangle(Brushes.Black, labelPos.X, labelPos.Y, size.Width + 4, size.Height);
        g.DrawString(label, LabelFont, Brushes.White, labelPos.X + 2, labelPos.Y);
    }

    protected override void OnPaintBackground(PaintEventArgs e) { /* fully painted in OnPaint */ }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _screenshot?.Dispose();
        base.Dispose(disposing);
    }
}

/// <summary>Adapts the reusable <see cref="OverlayForm"/> to the platform-neutral selector interface.</summary>
public sealed class OverlayRegionSelector : IRegionSelector, IDisposable
{
    private readonly OverlayForm _form;
    private readonly SynchronizationContext _ui;

    public OverlayRegionSelector(SynchronizationContext ui)
    {
        _ui = ui;
        _form = new OverlayForm();
        // Force handle creation now so the first Show() is instant.
        _ = _form.Handle;
    }

    public Task<PixelRect?> SelectAsync(ScreenCapture capture, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<PixelRect?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ui.Post(async _ =>
        {
            try { tcs.TrySetResult(await _form.BeginSelectAsync(capture, cancellationToken)); }
            catch (Exception ex) { tcs.TrySetException(ex); }
        }, null);
        return tcs.Task;
    }

    public void Dispose() => _form.Dispose();
}
