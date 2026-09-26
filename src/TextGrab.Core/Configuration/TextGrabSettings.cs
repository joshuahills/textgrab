using System.Text.Json;
using System.Text.Json.Serialization;

namespace TextGrab.Configuration;

public sealed class TextGrabSettings
{
    public string Hotkey { get; set; } = "Ctrl+Shift+X";

    /// <summary>Name of the preferred <c>IOcrEngine</c>. Falls back to the first available engine.</summary>
    public string OcrEngine { get; set; } = "windows";

    /// <summary>Selections shorter than this (in pixels) are upscaled before OCR to help with small text.</summary>
    public int MinOcrHeight { get; set; } = 64;

    /// <summary>Maximum upscale factor applied to small selections.</summary>
    public double MaxUpscale { get; set; } = 3.0;

    /// <summary>Join recognised lines with a single space instead of newlines (useful for subtitles).</summary>
    public bool JoinLines { get; set; } = false;

    /// <summary>Show a brief on-screen confirmation after copying.</summary>
    public bool ShowConfirmation { get; set; } = true;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TextGrab", "settings.json");

    public static TextGrabSettings Load(string? path = null)
    {
        path ??= DefaultPath;
        if (!File.Exists(path))
        {
            var defaults = new TextGrabSettings();
            defaults.Save(path);
            return defaults;
        }
        return JsonSerializer.Deserialize<TextGrabSettings>(File.ReadAllText(path), JsonOptions) ?? new TextGrabSettings();
    }

    public void Save(string? path = null)
    {
        path ??= DefaultPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
    }
}
