using NearbyPanel.Core;
using Xunit;

namespace NearbyPanel.Tests;

public class EggsTests
{
    private static EggReport Warm(float? progress, float? left) =>
        new(Stack: 1, Warm: true, Progress: progress, SecondsLeft: left,
            NeedsFire: true, HasFire: true, NeedsRoof: true, UnderRoof: true, Cover: 1f, RequiredCover: 0.7f);

    private static EggReport Cold(bool hasFire, bool underRoof, float cover) =>
        new(Stack: 1, Warm: false, Progress: null, SecondsLeft: null,
            NeedsFire: true, HasFire: hasFire, NeedsRoof: true, UnderRoof: underRoof, Cover: cover, RequiredCover: 0.7f);

    [Theory]
    [InlineData(0.7f)]
    [InlineData(0.699f)]
    public void The_required_cover_is_rounded_not_truncated(float required)
    {
        EggReport report = Cold(hasFire: true, underRoof: true, cover: 0.45f) with { RequiredCover = required };
        Assert.EndsWith("< 70%", Eggs.Describe(report, ProgressFormat.Both));
    }

    [Fact]
    public void A_stack_never_incubates()
    {
        EggReport stacked = Warm(0.5f, 10f) with { Stack = 3 };
        Assert.Equal("stacked ×3", Eggs.Describe(stacked, ProgressFormat.Both));
    }

    [Theory]
    [InlineData(ProgressFormat.Percent, "warm · 62%")]
    [InlineData(ProgressFormat.Time, "warm · hatches in 18 min")]
    [InlineData(ProgressFormat.Both, "warm · 62% · 18 min")]
    public void A_warm_egg_shows_its_progress_in_the_chosen_format(ProgressFormat format, string expected)
    {
        Assert.Equal(expected, Eggs.Describe(Warm(0.62f, 18f * 60f), format));
    }

    [Fact]
    public void A_warm_egg_without_a_clock_reads_just_warm()
    {
        Assert.Equal("warm", Eggs.Describe(Warm(null, null), ProgressFormat.Both));
    }

    [Fact]
    public void Progress_past_the_end_clamps()
    {
        Assert.Equal("warm · 100% · 0 s", Eggs.Describe(Warm(1.2f, -3f), ProgressFormat.Both));
    }

    [Fact]
    public void A_cold_egg_names_every_missing_condition()
    {
        Assert.Equal("cold: no fire, no roof", Eggs.Describe(Cold(hasFire: false, underRoof: false, cover: 0f), ProgressFormat.Both));
        Assert.Equal("cold: no fire", Eggs.Describe(Cold(hasFire: false, underRoof: true, cover: 1f), ProgressFormat.Both));
        Assert.Equal("cold: no roof", Eggs.Describe(Cold(hasFire: true, underRoof: false, cover: 0.9f), ProgressFormat.Both));
    }

    [Fact]
    public void A_roof_with_too_little_cover_shows_both_numbers()
    {
        Assert.Equal("cold: roof 45% < 70%", Eggs.Describe(Cold(hasFire: true, underRoof: true, cover: 0.45f), ProgressFormat.Both));
    }

    [Fact]
    public void A_cold_egg_with_every_condition_met_is_waiting_for_the_owner()
    {
        // GrowStart is stamped by the owner every 5-10 s; until then the egg is cold for no reason we can name.
        Assert.Equal("cold", Eggs.Describe(Cold(hasFire: true, underRoof: true, cover: 1f), ProgressFormat.Both));
    }

    [Fact]
    public void Conditions_the_species_does_not_require_are_not_reported()
    {
        EggReport report = Cold(hasFire: false, underRoof: false, cover: 0f) with { NeedsFire = false, NeedsRoof = false };
        Assert.Equal("cold", Eggs.Describe(report, ProgressFormat.Both));
    }
}
