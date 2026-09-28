using NearbyPanel.Core;
using Xunit;

namespace NearbyPanel.Tests;

public class DirectionFormatTests
{
    private static readonly Vec3 Origin = new(0f, 0f, 0f);
    private static readonly Vec3 North = new(0f, 0f, 1f);
    private static readonly Vec3 East = new(1f, 0f, 0f);

    private static NearbyEntity At(float x, float z) =>
        new("Boar", EntityKind.Tameable, new Vec3(x, 0f, z), 1, null, null);

    // ----- world compass ---------------------------------------------------

    [Theory]
    [InlineData(0f, 10f, "N")]
    [InlineData(10f, 10f, "NE")]
    [InlineData(10f, 0f, "E")]
    [InlineData(10f, -10f, "SE")]
    [InlineData(0f, -10f, "S")]
    [InlineData(-10f, -10f, "SW")]
    [InlineData(-10f, 0f, "W")]
    [InlineData(-10f, 10f, "NW")]
    public void CompassPoint_names_the_world_direction(float x, float z, string expected)
    {
        Assert.Equal(expected, Geometry.CompassPoint(Geometry.CompassBearing(Origin, new Vec3(x, 0f, z))));
    }

    [Fact]
    public void Compass_does_not_change_when_the_viewer_turns()
    {
        // The whole point of the mode: it matches the map, not the screen.
        NearbyEntity target = At(0f, 10f);

        Assert.Equal("N", RowFormatter.Cells(target, Origin, North, DirectionFormat.Compass).Direction);
        Assert.Equal("N", RowFormatter.Cells(target, Origin, East, DirectionFormat.Compass).Direction);
    }

    [Fact]
    public void CompassBearing_is_zero_when_the_positions_coincide()
    {
        Assert.Equal(0f, Geometry.CompassBearing(Origin, new Vec3(0f, 20f, 0f)), 3);
    }

    // ----- degrees ---------------------------------------------------------

    [Theory]
    [InlineData(0f, "0°")]
    [InlineData(90f, "90°")]
    [InlineData(-90f, "-90°")]
    [InlineData(180f, "180°")]
    [InlineData(45.4f, "45°")]
    [InlineData(45.6f, "46°")]
    [InlineData(-45.6f, "-46°")]
    public void DegreesLabel_reads_as_an_angle(float bearing, string expected)
    {
        Assert.Equal(expected, Geometry.DegreesLabel(bearing));
    }

    [Fact]
    public void Directly_behind_is_180_not_minus_180()
    {
        Assert.Equal("180°", Geometry.DegreesLabel(-180f));
        Assert.Equal("180°", Geometry.DegreesLabel(180f));
    }

    [Fact]
    public void DegreesLabel_survives_a_nonsense_angle()
    {
        Assert.Equal("?", Geometry.DegreesLabel(float.NaN));
    }

    [Fact]
    public void Degrees_turn_with_the_viewer()
    {
        NearbyEntity target = At(0f, 10f); // due north

        Assert.Equal("0°", RowFormatter.Cells(target, Origin, North, DirectionFormat.Degrees).Direction);
        Assert.Equal("-90°", RowFormatter.Cells(target, Origin, East, DirectionFormat.Degrees).Direction);
    }

    [Fact]
    public void Something_behind_you_reads_180_degrees()
    {
        // The example that prompted the option.
        NearbyEntity behind = At(0f, -10f);

        Assert.Equal("180°", RowFormatter.Cells(behind, Origin, North, DirectionFormat.Degrees).Direction);
    }

    // ----- relative letters ------------------------------------------------

    [Fact]
    public void Relative_letters_turn_with_the_viewer()
    {
        NearbyEntity target = At(0f, 10f);

        Assert.Equal("F", RowFormatter.Cells(target, Origin, North, DirectionFormat.Relative).Direction);
        Assert.Equal("L", RowFormatter.Cells(target, Origin, East, DirectionFormat.Relative).Direction);
    }

    // ----- arrows ----------------------------------------------------------

    [Theory]
    [InlineData(0f, "↑")]
    [InlineData(45f, "↗")]
    [InlineData(90f, "→")]
    [InlineData(135f, "↘")]
    [InlineData(180f, "↓")]
    [InlineData(-180f, "↓")]
    [InlineData(-135f, "↙")]
    [InlineData(-90f, "←")]
    [InlineData(-45f, "↖")]
    [InlineData(22.4f, "↑")]  // each arrow owns a symmetric 45° sector
    [InlineData(22.5f, "↗")]
    [InlineData(-22.4f, "↑")]
    [InlineData(-22.6f, "↖")]
    [InlineData(405f, "↗")]   // any angle folds back into range
    public void RelativeArrow_points_where_the_target_is_on_screen(float bearing, string expected)
    {
        Assert.Equal(expected, Geometry.RelativeArrow(bearing));
    }

    [Fact]
    public void RelativeArrow_survives_a_nonsense_angle()
    {
        Assert.Equal("?", Geometry.RelativeArrow(float.NaN));
    }

    [Fact]
    public void Arrows_turn_with_the_viewer()
    {
        // Rotating the viewer is what exercises the bearing, not just the lookup.
        NearbyEntity target = At(0f, 10f); // due north

        Assert.Equal("↑", RowFormatter.Cells(target, Origin, North, DirectionFormat.Arrow).Direction);
        Assert.Equal("←", RowFormatter.Cells(target, Origin, East, DirectionFormat.Arrow).Direction);
    }

    [Fact]
    public void Arrows_and_letters_agree_on_the_sector()
    {
        // Same eight sectors, so a creature cannot be "FR" in one format and "→" in the other.
        string[] letters = { "F", "FR", "R", "BR", "B", "BL", "L", "FL" };
        string[] arrows = { "↑", "↗", "→", "↘", "↓", "↙", "←", "↖" };

        for (float bearing = -179f; bearing <= 180f; bearing += 0.5f)
        {
            int index = System.Array.IndexOf(letters, Geometry.RelativeHeading(bearing));
            Assert.Equal(arrows[index], Geometry.RelativeArrow(bearing));
        }
    }

    // ----- layout ----------------------------------------------------------

    [Fact]
    public void The_DIR_column_is_wide_enough_for_the_longest_degrees_label()
    {
        // "-135°" is five characters; a narrower column would truncate it.
        NearbyEntity behindLeft = At(-10f, -10f);

        string row = RowFormatter.Row(behindLeft, Origin, North, DirectionFormat.Degrees);
        int[] offsets = RowFormatter.ColumnOffsets();
        int width = offsets[3] - offsets[2] - 1;

        Assert.Equal("-135°", row.Substring(offsets[2], width).Trim());
    }

    [Fact]
    public void Progress_is_flagged_on_the_cells_rather_than_inferred_from_the_text()
    {
        NearbyEntity taming = new("Boar", EntityKind.Tameable, new Vec3(0f, 0f, 5f), 1, "Hungry", 0.42f);
        NearbyEntity tamed = new("Fenrir", EntityKind.Tameable, new Vec3(0f, 0f, 5f), 2, "Happy", null);

        Assert.True(RowFormatter.Cells(taming, Origin, North).Progress);
        Assert.False(RowFormatter.Cells(tamed, Origin, North).Progress);
    }
}
