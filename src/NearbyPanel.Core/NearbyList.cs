using System;
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
    ///
    /// Distance here is the full 3-D distance, matching the game's own range query,
    /// so this filter can never promise a reach the game has already withheld. The
    /// distance *shown* is the ground distance, which is the useful one for walking
    /// towards something.
    ///
    /// Ties break on name, so the list does not flicker between refreshes.
    /// </summary>
    public static IReadOnlyList<NearbyEntity> Build(
        IEnumerable<NearbyEntity> found,
        Vec3 viewer,
        float radius,
        int limit,
        Func<NearbyEntity, bool>? include = null)
    {
        IEnumerable<NearbyEntity> query = found
            .Where(e => Geometry.Distance(viewer, e.Position) <= radius);

        if (include != null)
        {
            query = query.Where(include);
        }

        return query
            .OrderBy(e => Geometry.Distance(viewer, e.Position))
            .ThenBy(e => e.Name, StringComparer.Ordinal)
            .Take(limit)
            .ToList();
    }

    /// <summary>
    /// Creatures carrying a tameable component — which includes the ones you have
    /// already tamed, so your own pets appear in the list too. The default view.
    /// </summary>
    public static bool IsTameable(NearbyEntity entity) => entity.Kind == EntityKind.Tameable;
}
