namespace TextGrab.Platform;

[Flags]
public enum HotkeyModifiers { None = 0, Alt = 1, Control = 2, Shift = 4, Super = 8 }

/// <summary>A key chord such as Ctrl+Shift+X. <see cref="Key"/> is a case-insensitive key name ("X", "F9", "Space").</summary>
public sealed record HotkeyGesture(HotkeyModifiers Modifiers, string Key)
{
    public static HotkeyGesture Parse(string text)
    {
        var parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) throw new FormatException("Empty hotkey.");
        var mods = HotkeyModifiers.None;
        foreach (var p in parts[..^1])
        {
            mods |= p.ToLowerInvariant() switch
            {
                "ctrl" or "control" => HotkeyModifiers.Control,
                "alt" => HotkeyModifiers.Alt,
                "shift" => HotkeyModifiers.Shift,
                "win" or "super" or "cmd" or "meta" => HotkeyModifiers.Super,
                _ => throw new FormatException($"Unknown modifier '{p}'."),
            };
        }
        return new HotkeyGesture(mods, parts[^1]);
    }

    public override string ToString()
    {
        var parts = new List<string>();
        if (Modifiers.HasFlag(HotkeyModifiers.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(HotkeyModifiers.Super)) parts.Add("Win");
        parts.Add(Key);
        return string.Join("+", parts);
    }
}

public interface IHotkeyService : IDisposable
{
    /// <summary>Registers a system-wide hotkey. Throws if the chord is taken by another app.</summary>
    void Register(HotkeyGesture gesture, Action onPressed);
}
