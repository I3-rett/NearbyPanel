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
        EntityKind kind = EntityKind.Creature,
        float? progress = null) =>
        new(name, kind, new Vec3(x, 0f, 0f), 1, null, progress);

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
    public void Build_caps_at_the_limit_keeping_the_closest()
    {
        List<NearbyEntity> found = new() { At("a", 1f), At("b", 2f), At("c", 3f) };

        var rows = NearbyList.Build(found, Viewer, radius: 50f, limit: 2);

        Assert.Equal(new[] { "a", "b" }, rows.Select(r => r.Name));
    }

    [Fact]
    public void Build_breaks_distance_ties_on_name_so_the_order_is_stable()
    {
        List<NearbyEntity> found = new() { At("zebra", 10f), At("aardvark", 10f) };

        var rows = NearbyList.Build(found, Viewer, radius: 50f, limit: 10);

        Assert.Equal(new[] { "aardvark", "zebra" }, rows.Select(r => r.Name));
    }

    [Fact]
    public void Build_applies_the_filter()
    {
        List<NearbyEntity> found = new()
        {
            At("boar", 5f, EntityKind.Tameable),
            At("greydwarf", 6f),
        };

        var rows = NearbyList.Build(found, Viewer, 50f, 10, NearbyList.IsTameable);

        Assert.Single(rows);
        Assert.Equal("boar", rows[0].Name);
    }

    [Fact]
    public void Build_returns_empty_when_nothing_is_nearby()
    {
        Assert.Empty(NearbyList.Build(new List<NearbyEntity>(), Viewer, 50f, 10));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(0f, false)]
    [InlineData(0.5f, true)]
    [InlineData(1f, false)]
    public void IsBeingTamed_only_covers_taming_in_progress(float? progress, bool expected)
    {
        NearbyEntity entity = At("x", 1f, EntityKind.Tameable, progress);

        Assert.Equal(expected, NearbyList.IsBeingTamed(entity));
    }
}
