using System.Collections.Generic;
using System.Linq;

namespace NearbyPanel.Core;

/// <summary>
/// Turns a raw set of nearby entities into the ordered, filtered rows the panel
/// shows. Kept separate from the scan so the ordering rules can be tested without
/// a running game.
/// </summary>
public static class NearbyList
{
    /// <summary>
    /// Entities within <paramref name="radius"/> of the viewer that satisfy
    /// <paramref name="include"/>, nearest first, capped at <paramref name="limit"/>.
    /// Ties break on name so the order never flickers between refreshes.
    /// </summary>
    public static IReadOnlyList<NearbyEntity> Build(
        IEnumerable<NearbyEntity> found,
        Vec3 viewer,
        float radius,
        int limit,
        System.Func<NearbyEntity, bool>? include = null)
    {
        IEnumerable<NearbyEntity> query = found
            .Where(e => Geometry.GroundDistance(viewer, e.Position) <= radius);

        if (include != null)
        {
            query = query.Where(include);
        }

        return query
            .OrderBy(e => Geometry.GroundDistance(viewer, e.Position))
            .ThenBy(e => e.Name, System.StringComparer.Ordinal)
            .Take(limit)
            .ToList();
    }

    /// <summary>Only creatures that can be tamed. The default view.</summary>
    public static bool IsTameable(NearbyEntity entity) => entity.Kind == EntityKind.Tameable;

    /// <summary>Tameables with taming under way but not finished.</summary>
    public static bool IsBeingTamed(NearbyEntity entity) =>
        entity.TamingProgress is > 0f and < 1f;
}
