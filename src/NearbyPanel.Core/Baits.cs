using System.Collections.Generic;
using System.Linq;

namespace NearbyPanel.Core;

/// <summary>One bait a fish species accepts, and the chance it bites on it (0..1).</summary>
public readonly record struct Bait(string Name, float Chance);

/// <summary>The STATUS cell of a fish row: what it bites on.</summary>
public static class Baits
{
    /// <summary>Shown when the fish has no bait the game could ever pick.</summary>
    public const string None = "no bait";

    /// <summary>
    /// The baits in the game's order, each with its chance when that is below
    /// certain. A bait with no name or no chance is left out: the game can never
    /// pick it, so listing it would send the player after bait that does nothing.
    /// Not sorted or deduplicated, so the text matches what the game holds.
    /// </summary>
    public static string Describe(IEnumerable<Bait> baits)
    {
        List<string> parts = baits
            .Where(b => !string.IsNullOrWhiteSpace(b.Name) && b.Chance > 0f)
            .Select(b => b.Chance < 1f ? b.Name + " " + RowFormatter.Percent(b.Chance) : b.Name)
            .ToList();

        return parts.Count == 0 ? None : string.Join(", ", parts);
    }
}
