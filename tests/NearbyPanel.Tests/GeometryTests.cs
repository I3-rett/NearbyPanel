using NearbyPanel.Core;
using Xunit;

namespace NearbyPanel.Tests;

public class GeometryTests
{
    private static readonly Vec3 Origin = new(0f, 0f, 0f);
    private static readonly Vec3 North = new(0f, 0f, 1f);

    [Fact]
    public void GroundDistance_ignores_height()
    {
        Vec3 high = new(3f, 100f, 4f);
        Assert.Equal(5f, Geometry.GroundDistance(Origin, high), 3);
    }

    [Fact]
    public void HeightDelta_is_signed()
    {
        Assert.Equal(12f, Geometry.HeightDelta(Origin, new Vec3(0f, 12f, 0f)), 3);
        Assert.Equal(-12f, Geometry.HeightDelta(new Vec3(0f, 12f, 0f), Origin), 3);
    }

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

    [Theory]
    [InlineData(0f, "N")]
    [InlineData(45f, "NE")]
    [InlineData(90f, "E")]
    [InlineData(180f, "S")]
    [InlineData(-90f, "W")]
    [InlineData(-135f, "SW")]
    [InlineData(359f, "N")]
    public void CompassPoint_maps_bearings_to_eight_points(float bearing, string expected)
    {
        Assert.Equal(expected, Geometry.CompassPoint(bearing));
    }
}
