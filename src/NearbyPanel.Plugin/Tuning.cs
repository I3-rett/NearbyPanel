namespace NearbyPanel;

/// <summary>
/// Values fixed in the source rather than exposed as settings.
///
/// Reach is what separates a short-range awareness tool from surveillance, so the
/// radius is not something anyone can widen at runtime. A limit that can be slid
/// is not a limit. The list itself covers every creature inside that radius, and a
/// text filter narrows it when it gets busy.
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
