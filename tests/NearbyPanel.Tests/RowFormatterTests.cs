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
    public void Percent_does_not_emit_garbage_for_NaN()
    {
        // (int)(NaN * 100f) is an undefined conversion and yields int.MinValue on
        // x64, which would put "-2147483648%" in the STATUS column.
        Assert.Equal("?", RowFormatter.Percent(float.NaN));
    }

    [Theory]
    [InlineData(1, "")]       // an ordinary creature is level 1, which is no stars
    [InlineData(2, "1")]
    [InlineData(3, "2")]
    [InlineData(0, "")]
    public void Stars_is_the_level_minus_one(int level, string expected)
    {
        Assert.Equal(expected, RowFormatter.Stars(level));
    }

    [Fact]
    public void Truncate_leaves_short_names_alone()
    {
        Assert.Equal("Boar", RowFormatter.Truncate("Boar", 20));
    }

    [Fact]
    public void Truncate_keeps_a_name_of_exactly_the_budget()
    {
        string exact = new('x', 20);
        Assert.Equal(exact, RowFormatter.Truncate(exact, 20));
    }

    [Fact]
    public void Truncate_ellipsises_long_names_within_the_budget()
    {
        string result = RowFormatter.Truncate("AnImplausiblyLongCreatureName", 10);

        Assert.Equal(10, result.Length);
        Assert.EndsWith("…", result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Truncate_returns_empty_rather_than_throwing_on_a_useless_budget(int max)
    {
        // Substring(0, max - 1) used to throw here.
        Assert.Equal(string.Empty, RowFormatter.Truncate("Boar", max));
    }

    [Fact]
    public void Truncate_at_one_character_is_just_the_ellipsis()
    {
        Assert.Equal("…", RowFormatter.Truncate("Boar", 1));
    }

    // ----- layout ---------------------------------------------------------

    [Fact]
    public void Header_and_row_agree_on_where_every_column_starts()
    {
        // The header and the row used to be two hand-written literals, and every
        // column after DIST sat one or two characters right of its own data.
        NearbyEntity entity = new("Boar", EntityKind.Tameable, new Vec3(0f, 0f, 10f), 3, "Hungry", 0.42f);

        string header = RowFormatter.Header;
        string row = RowFormatter.Row(entity, Viewer, North);
        int[] offsets = RowFormatter.ColumnOffsets();
        string[] names = RowFormatter.ColumnNames;

        for (int i = 0; i < names.Length; i++)
        {
            int at = offsets[i];

            // Each column occupies the same character span in both lines. A
            // right-aligned column pads its heading exactly as it pads its value,
            // so the two still line up; what must never happen is the span itself
            // differing, which is how the header ended up a column adrift before.
            int width = i + 1 < offsets.Length ? offsets[i + 1] - offsets[i] - 1 : 0;

            if (width <= 0)
            {
                Assert.Equal(names[i], header.Substring(at).Trim());
                continue;
            }

            Assert.True(at + width <= header.Length, $"header too short for {names[i]}");
            Assert.Equal(names[i], header.Substring(at, width).Trim());
            Assert.True(at + width <= row.Length, $"row too short for {names[i]}");
        }
    }

    [Fact]
    public void Row_places_each_value_inside_its_own_column()
    {
        NearbyEntity entity = new("Boar", EntityKind.Tameable, new Vec3(0f, 0f, 10f), 3, "Hungry", 0.42f);

        string row = RowFormatter.Row(entity, Viewer, North);
        int[] offsets = RowFormatter.ColumnOffsets();

        // NAME is left-aligned at offset 0; STATUS is the last column, index 6.
        Assert.StartsWith("Boar", row);
        Assert.Equal("42% Hungry", row.Substring(offsets[offsets.Length - 1]));
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
    public void Row_truncates_an_over_long_name_to_its_column()
    {
        NearbyEntity entity = new(
            new string('x', 60), EntityKind.Creature, new Vec3(0f, 0f, 10f), 1, null, null);

        string row = RowFormatter.Row(entity, Viewer, North);
        int[] offsets = RowFormatter.ColumnOffsets();

        // The DIST column must still start where the header says it does.
        Assert.Equal("10.0", row.Substring(offsets[1], 6).Trim());
    }
}
