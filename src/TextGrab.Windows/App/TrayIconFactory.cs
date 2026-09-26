using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace TextGrab.Windows.App;

/// <summary>Draws the tray icon at runtime so the repo needs no binary assets.</summary>
internal static class TrayIconFactory
{
    public static Icon Create()
    {
        const int size = 32;
        using var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.Clear(Color.Transparent);

            using var back = new SolidBrush(Color.FromArgb(0, 120, 215));
            g.FillRoundedRectangle(back, new Rectangle(1, 1, size - 2, size - 2), new Size(8, 8));

            using var font = new Font("Segoe UI", 18f, FontStyle.Bold, GraphicsUnit.Pixel);
            var text = "T";
            var textSize = g.MeasureString(text, font);
            g.DrawString(text, font, Brushes.White, (size - textSize.Width) / 2f, (size - textSize.Height) / 2f);

            using var pen = new Pen(Color.White, 2f);
            g.DrawLine(pen, 6, size - 6, 12, size - 6);
            g.DrawLine(pen, size - 12, size - 6, size - 6, size - 6);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }
}
