using System.ComponentModel;
using System.Runtime.InteropServices;
using TextGrab.Platform;
using TextGrab.Windows.Interop;

namespace TextGrab.Windows.Platform;

/// <summary>Global hotkeys via RegisterHotKey on a hidden message-only window.</summary>
public sealed class Win32HotkeyService : NativeWindow, IHotkeyService
{
    private readonly Dictionary<int, Action> _handlers = new();
    private int _nextId = 1;

    public Win32HotkeyService()
    {
        CreateHandle(new CreateParams { Caption = "TextGrab.Hotkeys", Parent = -3 /* HWND_MESSAGE */ });
    }

    public void Register(HotkeyGesture gesture, Action onPressed)
    {
        var id = _nextId++;
        var vk = ResolveKey(gesture.Key);
        var mods = MOD(gesture.Modifiers) | Native.MOD_NOREPEAT;
        if (!Native.RegisterHotKey(Handle, id, mods, vk))
        {
            var err = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"Could not register hotkey {gesture}. It is probably in use by another application. ({new Win32Exception(err).Message})");
        }
        _handlers[id] = onPressed;
    }

    private static uint MOD(HotkeyModifiers m)
    {
        uint r = 0;
        if (m.HasFlag(HotkeyModifiers.Alt)) r |= Native.MOD_ALT;
        if (m.HasFlag(HotkeyModifiers.Control)) r |= Native.MOD_CONTROL;
        if (m.HasFlag(HotkeyModifiers.Shift)) r |= Native.MOD_SHIFT;
        if (m.HasFlag(HotkeyModifiers.Super)) r |= Native.MOD_WIN;
        return r;
    }

    private static uint ResolveKey(string name)
    {
        var normalised = name.Trim() switch
        {
            { Length: 1 } s when char.IsDigit(s[0]) => "D" + s,
            "Esc" => "Escape",
            "PrintScreen" or "PrtSc" => "PrintScreen",
            var s => s,
        };
        if (!Enum.TryParse<Keys>(normalised, ignoreCase: true, out var key))
            throw new FormatException($"Unknown key '{name}'.");
        return (uint)key;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == Native.WM_HOTKEY && _handlers.TryGetValue((int)m.WParam, out var handler))
        {
            handler();
            return;
        }
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        foreach (var id in _handlers.Keys) Native.UnregisterHotKey(Handle, id);
        _handlers.Clear();
        DestroyHandle();
    }
}
