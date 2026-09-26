using TextGrab.Platform;
using Xunit;

namespace TextGrab.Tests;

public class HotkeyGestureTests
{
    [Theory]
    [InlineData("Ctrl+Shift+X", HotkeyModifiers.Control | HotkeyModifiers.Shift, "X")]
    [InlineData("alt + f9", HotkeyModifiers.Alt, "f9")]
    [InlineData("Win+PrintScreen", HotkeyModifiers.Super, "PrintScreen")]
    public void Parse_ReadsModifiersAndKey(string text, HotkeyModifiers mods, string key)
    {
        var g = HotkeyGesture.Parse(text);
        Assert.Equal(mods, g.Modifiers);
        Assert.Equal(key, g.Key);
    }

    [Fact]
    public void Parse_RejectsUnknownModifier() => Assert.Throws<FormatException>(() => HotkeyGesture.Parse("Hyper+X"));
}
