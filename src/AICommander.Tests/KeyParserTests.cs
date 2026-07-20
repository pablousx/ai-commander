using AICommander.Core.Utilities;

namespace AICommander.Tests;

public class KeyParserTests
{
    [Theory]
    [InlineData("Enter", 0x0D)]
    [InlineData("tab", 0x09)]
    [InlineData("ESCAPE", 0x1B)]
    [InlineData("space", 0x20)]
    [InlineData("A", 0x41)]
    [InlineData("Z", 0x5A)]
    [InlineData("0", 0x30)]
    [InlineData("9", 0x39)]
    [InlineData("F1", 0x70)]
    [InlineData("F12", 0x7B)]
    [InlineData("LeftBracket", 0xDB)]
    [InlineData("[", 0xDB)]
    public void ParseKey_ValidKeys_ReturnsCorrectVkCode(string keyString, ushort expectedVk)
    {
        var result = KeyParser.ParseKey(keyString);
        Assert.Equal(expectedVk, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("UnknownKey")]
    [InlineData("Ctrl+Alt+Del")]
    public void ParseKey_InvalidOrEmptyKeys_ReturnsZero(string keyString)
    {
        var result = KeyParser.ParseKey(keyString);
        Assert.Equal(0, result);
    }

    [Fact]
    public void ParseHotkey_WithModifiersAndKey_ReturnsCorrectValues()
    {
        var keys = new[] { "ctrl", "alt", "win", "leftbracket" };
        var (modifiers, vk) = KeyParser.ParseHotkey(keys);

        Assert.Equal((uint)(0x0002 | 0x0001 | 0x0008), modifiers);
        Assert.Equal((uint)0xDB, vk);
    }

    [Fact]
    public void ParseHotkey_WithOnlyModifiers_ReturnsZeroVk()
    {
        var keys = new[] { "ctrl", "shift" };
        var (modifiers, vk) = KeyParser.ParseHotkey(keys);

        Assert.Equal((uint)(0x0002 | 0x0004), modifiers);
        Assert.Equal((uint)0, vk);
    }
}
