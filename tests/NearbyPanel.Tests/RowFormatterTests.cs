using NearbyPanel.Core;
using Xunit;

namespace NearbyPanel.Tests;

public class RowFormatterTests
{
    private static readonly Vec3 Viewer = new(0f, 0f, 0f);
    private static readonly Vec3 North = new(0f, 0f, 1f);

    [Theory]
    [InlineData(0f, "0%")]
    [InlineData(0.5f, "50%")]
    [InlineData(0.997f, "99%")]
    [InlineData(1f, "100%")]
    [InlineData(-0.2f, "0%")]
    [InlineData(1.4f, "100%")]
    public void Percent_truncates_like_the_game_and_clamps(float progress, string expected)
    {
        Assert.Equal(expected, RowFormatter.Percent(progress));
    }

    [Fact]
    public void Truncate_leaves_short_names_alone()
    {
        Assert.Equal("Boar", RowFormatter.Truncate("Boar", 20));
    }

    [Fact]
    public void Truncate_ellipsises_long_names_within_the_budget()
    {
        string result = RowFormatter.Truncate("AnImplausiblyLongCreatureName", 10);

        Assert.Equal(10, result.Length);
        Assert.EndsWith("…", result);
    }

    [Fact]
    public void Row_shows_percent_before_status_when_taming()
    {
        NearbyEntity boar = new("Boar", EntityKind.Tameable, new Vec3(0f, 0f, 10f), 2, "Hungry", 0.42f);

        string row = RowFormatter.Row(boar, Viewer, North);

        Assert.Contains("42%", row);
        Assert.Contains("Hungry", row);
        Assert.True(row.IndexOf("42%") < row.IndexOf("Hungry"));
    }

    [Fact]
    public void Row_omits_percent_when_there_is_no_taming_in_progress()
    {
        NearbyEntity wolf = new("Wolf", EntityKind.Creature, new Vec3(0f, 0f, 10f), 1, "Wild", null);

        Assert.DoesNotContain("%", RowFormatter.Row(wolf, Viewer, North));
    }

    [Fact]
    public void Row_pads_the_name_column_to_the_header_width()
    {
        NearbyEntity boar = new("Boar", EntityKind.Tameable, new Vec3(3f, 2f, 4f), 3, "Happy", null);

        string row = RowFormatter.Row(boar, Viewer, North);

        Assert.StartsWith("Boar                ", row);
        Assert.Contains("5.0", row);
    }
}
