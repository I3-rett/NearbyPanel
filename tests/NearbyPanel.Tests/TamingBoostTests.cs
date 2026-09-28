using NearbyPanel.Core;
using Xunit;

namespace NearbyPanel.Tests;

/// <summary>
/// <c>Tameable.DecreaseRemainingTime</c> (1.0.16) multiplies each 3 s tick by
/// <c>m_tamingBoostMultiplier</c> once per player in range carrying the TamingBoost
/// attribute — Brew of Animal Whispers. The stored seconds are unboosted, so the
/// real time left is them divided by the same factor.
/// </summary>
public class TamingBoostTests
{
    [Theory]
    [InlineData(240f, 0, 2f, 240f)]
    [InlineData(240f, 1, 2f, 120f)]
    [InlineData(240f, 2, 2f, 60f)]   // compounds: x2 per boosted player
    [InlineData(240f, 3, 2f, 30f)]
    [InlineData(240f, 1, 3f, 80f)]   // the multiplier is the prefab's, not a constant
    public void Boost_divides_the_time_left(float remaining, int boosted, float multiplier, float expected)
    {
        Assert.Equal(expected, TamingBoost.SecondsLeft(remaining, boosted, multiplier), 3);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    public void A_multiplier_that_cannot_speed_anything_up_is_ignored(float multiplier)
    {
        // The game would stall or run backwards; better to show the stored time.
        Assert.Equal(240f, TamingBoost.SecondsLeft(240f, 2, multiplier), 3);
    }

    [Fact]
    public void A_negative_count_is_no_boost()
    {
        Assert.Equal(240f, TamingBoost.SecondsLeft(240f, -1, 2f), 3);
    }
}
