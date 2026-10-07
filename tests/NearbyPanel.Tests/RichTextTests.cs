using NearbyPanel.Core;
using Xunit;

namespace NearbyPanel.Tests;

public class RichTextTests
{
    [Theory]
    [InlineData("<color=orange>Zil</color>", "Zil")]
    [InlineData("<color=#FF8800FF>Lord Reto</color>", "Lord Reto")]
    [InlineData("<b><i>Thungr</i></b>", "Thungr")]
    [InlineData("<size=14>Brenna</size> the Bold", "Brenna the Bold")]
    [InlineData("Fuling", "Fuling")]
    [InlineData("", "")]
    public void Strip_removes_every_tag_and_keeps_the_text(string input, string expected)
    {
        Assert.Equal(expected, RichText.Strip(input));
    }

    [Fact]
    public void Strip_keeps_a_lone_angle_bracket()
    {
        Assert.Equal("a < b", RichText.Strip("a < b"));
        Assert.Equal("<unclosed", RichText.Strip("<unclosed"));
    }

    [Fact]
    public void Strip_handles_a_tag_at_the_very_end()
    {
        Assert.Equal("Zil", RichText.Strip("Zil<color=orange>"));
    }
}
