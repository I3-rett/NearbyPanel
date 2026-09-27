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
    /// already tamed, so your own pets appear in the list.
    /// </summary>
    public static bool IsTameable(NearbyEntity entity) => entity.Kind == EntityKind.Tameable;

    /// <summary>
    /// Everything except other players. Creatures are the point; the position and
    /// distance of the people you play with is the part deliberately left out.
    /// </summary>
    public static bool IsCreature(NearbyEntity entity) => entity.Kind != EntityKind.Player;

    /// <summary>
    /// Whether <paramref name="entity"/> matches a free-text filter, compared
    /// case-insensitively against its name. An empty or whitespace filter matches
    /// everything, so clearing it restores the full list rather than emptying it.
    /// </summary>
    public static bool MatchesText(NearbyEntity entity, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        string needle = text!.Trim();
        string name = entity.Name ?? string.Empty;

        return name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// The predicate the panel and the console dump both use: creatures only, and
    /// matching the current text filter.
    /// </summary>
    public static Func<NearbyEntity, bool> Filter(string? text) =>
        entity => IsCreature(entity) && MatchesText(entity, text);
}
