using NearbyPanel.Core;
using Xunit;

namespace NearbyPanel.Tests;

public class BaitsTests
{
    [Fact]
    public void No_baits_reads_no_bait()
    {
        Assert.Equal("no bait", Baits.Describe(new Bait[0]));
    }

    [Fact]
    public void Baits_the_game_can_never_pick_read_no_bait()
    {
        Assert.Equal("no bait", Baits.Describe(new[] { new Bait("Worm", 0f), new Bait("Frost bait", -1f) }));
    }

    [Fact]
    public void A_sure_bait_shows_just_its_name()
    {
        Assert.Equal("Mistlands bait", Baits.Describe(new[] { new Bait("Mistlands bait", 1f) }));
    }

    [Fact]
    public void A_partial_chance_is_appended_as_a_percentage()
    {
        Assert.Equal(
            "Mistlands bait, Frost bait 50%",
            Baits.Describe(new[] { new Bait("Mistlands bait", 1f), new Bait("Frost bait", 0.5f) }));
    }

    [Fact]
    public void A_bait_without_a_name_is_skipped()
    {
        Assert.Equal(
            "Frost bait",
            Baits.Describe(new[] { new Bait(null!, 1f), new Bait("  ", 1f), new Bait("Frost bait", 1f) }));
    }

    [Fact]
    public void The_games_order_is_kept()
    {
        Assert.Equal(
            "B, A 25%, B 25%",
            Baits.Describe(new[] { new Bait("B", 1f), new Bait("A", 0.25f), new Bait("B", 0.25f) }));
    }
}
