using System.Collections.Generic;
using System.Linq;
using NearbyPanel.Core;
using Xunit;

namespace NearbyPanel.Tests;

public class NearbyListTests
{
    private static readonly Vec3 Viewer = new(0f, 0f, 0f);

    private static NearbyEntity At(
        string name,
        float x,
        float y = 0f,
        EntityKind kind = EntityKind.Creature) =>
        new(name, kind, new Vec3(x, y, 0f), 1, null, null);

    [Fact]
    public void Build_orders_nearest_first()
    {
        List<NearbyEntity> found = new() { At("far", 30f), At("near", 5f), At("mid", 15f) };

        var rows = NearbyList.Build(found, Viewer, radius: 50f, limit: 10);

        Assert.Equal(new[] { "near", "mid", "far" }, rows.Select(r => r.Name));
    }

    [Fact]
    public void Build_excludes_anything_beyond_the_radius()
    {
        List<NearbyEntity> found = new() { At("inside", 10f), At("outside", 60f) };

        var rows = NearbyList.Build(found, Viewer, radius: 50f, limit: 10);

        Assert.Single(rows);
        Assert.Equal("inside", rows[0].Name);
    }

    [Fact]
    public void Build_includes_an_entity_exactly_on_the_radius()
    {
        List<NearbyEntity> found = new() { At("edge", 50f) };

        Assert.Single(NearbyList.Build(found, Viewer, radius: 50f, limit: 10));
    }

    [Fact]
    public void Build_measures_in_three_dimensions_like_the_game_does()
    {
        // 45 m out and 25 m down is 51.5 m in 3-D: the game's own range query would
        // never hand it over, so the list must not claim it either.
        List<NearbyEntity> found = new() { At("below the cliff", 45f, -25f) };

        Assert.Empty(NearbyList.Build(found, Viewer, radius: 50f, limit: 10));
    }

    [Fact]
    public void Build_orders_by_three_dimensional_distance()
    {
        // Same ground distance, different heights.
        List<NearbyEntity> found = new() { At("high", 10f, 20f), At("level", 10f, 0f) };

        var rows = NearbyList.Build(found, Viewer, radius: 50f, limit: 10);

        Assert.Equal(new[] { "level", "high" }, rows.Select(r => r.Name));
    }

    [Fact]
    public void Build_caps_at_the_limit_keeping_the_closest()
    {
        List<NearbyEntity> found = new() { At("a", 1f), At("b", 2f), At("c", 3f) };

        var rows = NearbyList.Build(found, Viewer, radius: 50f, limit: 2);

        Assert.Equal(new[] { "a", "b" }, rows.Select(r => r.Name));
    }

    [Fact]
    public void Build_breaks_distance_ties_on_name_so_the_order_is_stable()
    {
        // The scan's enumeration order is not guaranteed stable between frames, so
        // without this the rows would swap places while nothing moved.
        List<NearbyEntity> found = new() { At("zebra", 10f), At("aardvark", 10f) };
        List<NearbyEntity> reversed = new() { At("aardvark", 10f), At("zebra", 10f) };

        Assert.Equal(
            NearbyList.Build(found, Viewer, 50f, 10).Select(r => r.Name),
            NearbyList.Build(reversed, Viewer, 50f, 10).Select(r => r.Name));
    }

    [Fact]
    public void Build_applies_the_filter()
    {
        List<NearbyEntity> found = new()
        {
            At("boar", 5f, kind: EntityKind.Tameable),
            At("greydwarf", 6f),
        };

        var rows = NearbyList.Build(found, Viewer, 50f, 10, NearbyList.IsTameable);

        Assert.Single(rows);
        Assert.Equal("boar", rows[0].Name);
    }

    [Fact]
    public void IsTameable_includes_animals_that_are_already_tamed()
    {
        // The kind comes from the component being present, not from taming state,
        // so your own pets appear in the list.
        NearbyEntity pet = new("Fenrir", EntityKind.Tameable, new Vec3(1f, 0f, 0f), 2, "Happy", null);

        Assert.True(NearbyList.IsTameable(pet));
    }

    [Fact]
    public void Build_returns_empty_when_nothing_is_nearby()
    {
        Assert.Empty(NearbyList.Build(new List<NearbyEntity>(), Viewer, 50f, 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Build_returns_empty_for_a_useless_limit(int limit)
    {
        List<NearbyEntity> found = new() { At("a", 1f) };

        Assert.Empty(NearbyList.Build(found, Viewer, 50f, limit));
    }

    [Fact]
    public void Build_drops_entities_at_an_unusable_position_rather_than_listing_them()
    {
        List<NearbyEntity> found = new()
        {
            new("broken", EntityKind.Creature, new Vec3(float.NaN, 0f, 0f), 1, null, null),
            At("fine", 5f),
        };

        var rows = NearbyList.Build(found, Viewer, 50f, 10);

        Assert.Single(rows);
        Assert.Equal("fine", rows[0].Name);
    }

    // ----- named first ----------------------------------------------------

    private static NearbyEntity Named(string name, float x) =>
        At(name, x) with { HasGivenName = true };

    [Fact]
    public void Named_first_puts_a_named_animal_above_a_nearer_unnamed_one()
    {
        // The boar pen: MAMA and PAPA sit behind a dozen piglets.
        List<NearbyEntity> found = new() { At("Boar", 2f), Named("MAMA", 12f), At("Boar", 4f) };

        var rows = NearbyList.Build(found, Viewer, radius: 50f, limit: 10, namedFirst: true);

        Assert.Equal(new[] { "MAMA", "Boar", "Boar" }, rows.Select(r => r.Name));
    }

    [Fact]
    public void Named_animals_are_still_nearest_first_among_themselves()
    {
        List<NearbyEntity> found = new() { Named("PAPA", 20f), At("Boar", 1f), Named("MAMA", 10f) };

        var rows = NearbyList.Build(found, Viewer, radius: 50f, limit: 10, namedFirst: true);

        Assert.Equal(new[] { "MAMA", "PAPA", "Boar" }, rows.Select(r => r.Name));
    }

    [Fact]
    public void Named_first_sorts_before_the_cap_so_a_named_animal_is_never_cut()
    {
        List<NearbyEntity> found = new() { At("Boar", 1f), At("Boar", 2f), At("Boar", 3f), Named("PAPA", 40f) };

        var rows = NearbyList.Build(found, Viewer, radius: 50f, limit: 2, namedFirst: true);

        Assert.Equal(new[] { "PAPA", "Boar" }, rows.Select(r => r.Name));
    }

    [Fact]
    public void Without_named_first_a_named_animal_takes_its_place_by_distance()
    {
        List<NearbyEntity> found = new() { Named("MAMA", 12f), At("Boar", 2f) };

        var rows = NearbyList.Build(found, Viewer, radius: 50f, limit: 10, namedFirst: false);

        Assert.Equal(new[] { "Boar", "MAMA" }, rows.Select(r => r.Name));
    }

    [Fact]
    public void Named_first_still_honours_the_radius()
    {
        List<NearbyEntity> found = new() { Named("MAMA", 60f), At("Boar", 2f) };

        var rows = NearbyList.Build(found, Viewer, radius: 50f, limit: 10, namedFirst: true);

        Assert.Equal(new[] { "Boar" }, rows.Select(r => r.Name));
    }
}
