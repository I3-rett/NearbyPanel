using System.Collections.Generic;
using System.Linq;
using NearbyPanel.Core;
using Xunit;

namespace NearbyPanel.Tests;

public class TextFilterTests
{
    private static readonly Vec3 Viewer = new(0f, 0f, 0f);

    private static NearbyEntity Named(string name, EntityKind kind = EntityKind.Creature) =>
        new(name, kind, new Vec3(1f, 0f, 0f), 1, null, null);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void An_empty_filter_matches_everything(string? filter)
    {
        // Clearing the filter must restore the full list, not empty it.
        Assert.True(NearbyList.MatchesText(Named("Greydwarf"), filter));
    }

    [Theory]
    [InlineData("boar", true)]
    [InlineData("Boar", true)]
    [InlineData("BOAR", true)]
    [InlineData("oa", true)]
    [InlineData("wolf", false)]
    public void Matching_is_case_insensitive_and_partial(string filter, bool expected)
    {
        Assert.Equal(expected, NearbyList.MatchesText(Named("Boar"), filter));
    }

    [Fact]
    public void Surrounding_whitespace_in_the_filter_is_ignored()
    {
        Assert.True(NearbyList.MatchesText(Named("Boar"), "  boar  "));
    }

    [Fact]
    public void A_missing_name_simply_does_not_match()
    {
        Assert.False(NearbyList.MatchesText(Named(null!), "boar"));
    }

    [Fact]
    public void The_filter_matches_a_given_pet_name_too()
    {
        // The name column carries the given name for a tamed animal, so filtering
        // on it is how you find one particular pet.
        Assert.True(NearbyList.MatchesText(Named("Fenrir", EntityKind.Tameable), "fen"));
    }

    [Fact]
    public void IsCreature_excludes_other_players()
    {
        Assert.False(NearbyList.IsCreature(Named("BrEtHoR", EntityKind.Player)));
        Assert.True(NearbyList.IsCreature(Named("Boar", EntityKind.Tameable)));
        Assert.True(NearbyList.IsCreature(Named("Greydwarf")));
    }

    [Fact]
    public void The_combined_filter_keeps_creatures_and_drops_players()
    {
        List<NearbyEntity> found = new()
        {
            Named("Boar", EntityKind.Tameable),
            Named("Greydwarf"),
            Named("BrEtHoR", EntityKind.Player),
        };

        var rows = NearbyList.Build(found, Viewer, 50f, 10, NearbyList.Filter(null));

        Assert.Equal(new[] { "Boar", "Greydwarf" }, rows.Select(r => r.Name).OrderBy(n => n));
    }

    [Fact]
    public void The_combined_filter_applies_the_text_as_well()
    {
        List<NearbyEntity> found = new()
        {
            Named("Boar", EntityKind.Tameable),
            Named("Greydwarf"),
            Named("Greydwarf Elite"),
        };

        var rows = NearbyList.Build(found, Viewer, 50f, 10, NearbyList.Filter("grey"));

        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.StartsWith("Greydwarf", r.Name));
    }

    [Fact]
    public void A_player_is_excluded_even_when_the_text_matches_them()
    {
        List<NearbyEntity> found = new() { Named("BrEtHoR", EntityKind.Player) };

        Assert.Empty(NearbyList.Build(found, Viewer, 50f, 10, NearbyList.Filter("bre")));
    }

    [Theory]
    [InlineData("fish")]
    [InlineData("FISH")]
    [InlineData("perch")]
    public void The_fish_keyword_isolates_fish_without_knowing_their_names(string filter)
    {
        Assert.True(NearbyList.MatchesText(Named("Perch", EntityKind.Fish), filter));
    }

    [Fact]
    public void The_fish_keyword_does_not_match_a_creature()
    {
        Assert.False(NearbyList.MatchesText(Named("Boar"), "fish"));
    }
}
