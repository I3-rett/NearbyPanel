namespace NearbyPanel;

/// <summary>
/// Values that are deliberately not configurable. See
/// docs/adr/0003-fixed-radius-and-filters.md: the difference between a tool for
/// watching your own animals and a creature radar is the radius and the filter,
/// so neither is a setting anyone can widen at runtime.
/// </summary>
internal static class Tuning
{
    /// <summary>
    /// Metres, measured as a sphere because that is what the game's own range query
    /// uses. Comfortably inside the roughly 64-128 m within which the game
    /// instantiates entities at all, so the list is never truncated by loading.
    /// </summary>
    public const float ScanRadius = 50f;

    /// <summary>Most rows ever listed, closest first.</summary>
    public const int MaxRows = 25;

    /// <summary>
    /// The radius as it appears in text. A constant rather than a per-frame
    /// <c>ToString</c> of a constant.
    /// </summary>
    public const string RadiusLabel = "50";
}
