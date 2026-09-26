using NearbyPanel.Core;
using Xunit;

namespace NearbyPanel.Tests;

public class GeometryTests
{
    private static readonly Vec3 Origin = new(0f, 0f, 0f);
    private static readonly Vec3 North = new(0f, 0f, 1f);
    private static readonly Vec3 East = new(1f, 0f, 0f);

    [Fact]
    public void GroundDistance_ignores_height()
    {
        Vec3 high = new(3f, 100f, 4f);
        Assert.Equal(5f, Geometry.GroundDistance(Origin, high), 3);
    }

    [Fact]
    public void GroundDistance_works_from_a_non_origin_viewer()
    {
        Assert.Equal(5f, Geometry.GroundDistance(new Vec3(-3f, 7f, -4f), new Vec3(0f, 0f, 0f)), 3);
    }

    [Fact]
    public void Distance_includes_height()
    {
        // The metric the game's own range query uses, so the list filter must match it.
        Assert.Equal(13f, Geometry.Distance(Origin, new Vec3(3f, 12f, 4f)), 3);
    }

    [Fact]
    public void HeightDelta_is_signed()
    {
        Assert.Equal(12f, Geometry.HeightDelta(Origin, new Vec3(0f, 12f, 0f)), 3);
        Assert.Equal(-12f, Geometry.HeightDelta(new Vec3(0f, 12f, 0f), Origin), 3);
    }

    // ----- bearing, with the viewer facing north --------------------------

    [Fact]
    public void RelativeBearing_is_zero_straight_ahead()
    {
        Assert.Equal(0f, Geometry.RelativeBearing(Origin, North, new Vec3(0f, 0f, 10f)), 3);
    }

    [Fact]
    public void RelativeBearing_is_positive_to_the_right()
    {
        Assert.Equal(90f, Geometry.RelativeBearing(Origin, North, new Vec3(10f, 0f, 0f)), 3);
    }

    [Fact]
    public void RelativeBearing_is_negative_to_the_left()
    {
        Assert.Equal(-90f, Geometry.RelativeBearing(Origin, North, new Vec3(-10f, 0f, 0f)), 3);
    }

    [Fact]
    public void RelativeBearing_directly_behind_is_180_not_minus_180()
    {
        Assert.Equal(180f, Geometry.RelativeBearing(Origin, North, new Vec3(0f, 0f, -10f)), 3);
    }

    [Fact]
    public void RelativeBearing_is_zero_when_positions_coincide_horizontally()
    {
        Assert.Equal(0f, Geometry.RelativeBearing(Origin, North, new Vec3(0f, 50f, 0f)), 3);
    }

    // ----- bearing, with the viewer turned -------------------------------
    // Every test above passes forward = north, where Atan2(0, 1) is zero and the
    // facing term vanishes. These are the ones that actually exercise it: deleting
    // "- facing" or flipping its sign passes everything above and fails these.

    [Fact]
    public void RelativeBearing_accounts_for_the_direction_the_viewer_faces()
    {
        // Facing east, a target due north is 90 degrees to the LEFT.
        Assert.Equal(-90f, Geometry.RelativeBearing(Origin, East, new Vec3(0f, 0f, 10f)), 3);
    }

    [Fact]
    public void RelativeBearing_is_zero_when_the_target_is_along_the_facing_direction()
    {
        Assert.Equal(0f, Geometry.RelativeBearing(Origin, East, new Vec3(10f, 0f, 0f)), 3);
    }

    [Fact]
    public void RelativeBearing_turns_with_the_viewer()
    {
        Vec3 target = new(10f, 0f, 10f); // due north-east in world terms

        Assert.Equal(45f, Geometry.RelativeBearing(Origin, North, target), 3);
        Assert.Equal(-45f, Geometry.RelativeBearing(Origin, East, target), 3);
        Assert.Equal(135f, Geometry.RelativeBearing(Origin, new Vec3(-1f, 0f, 0f), target), 3);
    }

    [Fact]
    public void RelativeBearing_treats_a_degenerate_forward_as_facing_north()
    {
        // Reachable if a transform is read mid-destruction; must not throw.
        Assert.Equal(90f, Geometry.RelativeBearing(Origin, new Vec3(0f, 0f, 0f), new Vec3(10f, 0f, 0f)), 3);
    }

    // ----- heading labels -------------------------------------------------

    [Theory]
    [InlineData(0f, "F")]
    [InlineData(45f, "FR")]
    [InlineData(90f, "R")]
    [InlineData(135f, "BR")]
    [InlineData(180f, "B")]
    [InlineData(-135f, "BL")]
    [InlineData(-90f, "L")]
    [InlineData(-45f, "FL")]
    public void RelativeHeading_maps_bearings_to_eight_labels(float bearing, string expected)
    {
        Assert.Equal(expected, Geometry.RelativeHeading(bearing));
    }

    [Theory]
    [InlineData(22.4f, "F")]
    [InlineData(22.5f, "FR")]   // boundary belongs to the higher sector, symmetrically
    [InlineData(67.4f, "FR")]
    [InlineData(67.5f, "R")]
    [InlineData(-22.5f, "F")]
    [InlineData(-22.6f, "FL")]
    public void RelativeHeading_sector_boundaries_are_symmetric(float bearing, string expected)
    {
        Assert.Equal(expected, Geometry.RelativeHeading(bearing));
    }

    [Theory]
    [InlineData(360f, "F")]
    [InlineData(720f, "F")]
    [InlineData(-360f, "F")]
    [InlineData(405f, "FR")]
    public void RelativeHeading_folds_a_wrapped_angle(float bearing, string expected)
    {
        Assert.Equal(expected, Geometry.RelativeHeading(bearing));
    }

    [Theory]
    [InlineData(1e9f)]
    [InlineData(-1e9f)]
    [InlineData(float.MaxValue)]
    public void RelativeHeading_survives_an_absurd_angle(float bearing)
    {
        // Normalisation is modulo, not a loop: a loop would spin for billions of
        // iterations here and hang the game. The exact label does not matter, only
        // that it returns one promptly.
        Assert.Contains(Geometry.RelativeHeading(bearing), new[] { "F", "FR", "R", "BR", "B", "BL", "L", "FL" });
    }
}
