using System.Text;
using TextGrab.Configuration;
using TextGrab.Ocr;

namespace TextGrab.Pipeline;

public interface ITextPostProcessor
{
    string Format(OcrResult result);
}

/// <summary>Turns OCR lines into clipboard text: trims, drops empties, normalises whitespace.</summary>
public sealed class DefaultTextPostProcessor(TextGrabSettings settings) : ITextPostProcessor
{
    public string Format(OcrResult result)
    {
        var lines = result.Lines
            .Select(l => CollapseSpaces(l.Text).Trim())
            .Where(t => t.Length > 0)
            .ToList();

        if (lines.Count == 0) return string.Empty;
        return settings.JoinLines ? string.Join(' ', lines) : string.Join(Environment.NewLine, lines);
    }

    private static string CollapseSpaces(string s)
    {
        var sb = new StringBuilder(s.Length);
        var lastSpace = false;
        foreach (var ch in s)
        {
            var isSpace = char.IsWhiteSpace(ch);
            if (isSpace && lastSpace) continue;
            sb.Append(isSpace ? ' ' : ch);
            lastSpace = isSpace;
        }
        return sb.ToString();
    }
}
