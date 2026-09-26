using System.Reflection;

namespace TextGrab.Windows.App;

internal static class TrayIconFactory
{
    /// <summary>Loads the embedded application icon (also used as the exe icon and by the installer).</summary>
    public static Icon Create()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TextGrab.ico")
            ?? throw new InvalidOperationException("Embedded icon missing.");
        return new Icon(stream, 32, 32);
    }
}
