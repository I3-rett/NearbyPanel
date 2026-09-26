namespace NearbyPanel.Core;

/// <summary>
/// One row of the panel, already stripped of every game type. Built by the
/// adapter in the plugin; consumed by the sorting, filtering and formatting code
/// here. <paramref name="Status"/> holds a localized string the adapter resolved
/// (for tameables, the game's own $hud_tame* wording).
/// </summary>
/// <param name="TamingProgress">0..1 while taming is under way, null otherwise.</param>
public sealed record NearbyEntity(
    string Name,
    EntityKind Kind,
    Vec3 Position,
    int Level,
    string? Status,
    float? TamingProgress);
