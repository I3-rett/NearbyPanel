namespace NearbyPanel.Core;

/// <summary>
/// One row of the panel, already stripped of every game type. Built by the
/// adapter in the plugin; consumed by the sorting, filtering and formatting code
/// here. <paramref name="Status"/> holds a localized string the adapter resolved
/// (for tameables, the game's own $hud_tame* wording).
/// </summary>
/// <param name="TamingProgress">0..1 while taming is under way, null otherwise.</param>
/// <param name="GrowthProgress">
/// 0..1 while a young animal is growing towards its adult form, null when it has no
/// growth stage or the record cannot answer yet. Shares the status column with
/// <paramref name="TamingProgress"/>: a creature reports one percentage at a time.
/// </param>
/// <param name="Awareness">Whether it has noticed anything. Replicated, so always readable.</param>
/// <param name="Hostile">Whether it would attack you, from factions and aggravation.</param>
/// <param name="TargetsYou">
/// True when it is known to be targeting you, false when it is known not to be, and
/// <c>null</c> when it cannot be known — a creature's target is held only by the peer
/// simulating it, so one owned by the server or another player cannot answer. Null
/// must be shown as "unknown", never as "no": telling someone nothing is hunting them
/// when something might be is the worst failure this panel has.
/// </param>
/// <param name="HasGivenName">
/// True when a player has named it, so <paramref name="Name"/> is that name rather
/// than the species. Named animals are the ones people come looking for.
/// </param>
public sealed record NearbyEntity(
    string Name,
    EntityKind Kind,
    Vec3 Position,
    int Level,
    string? Status,
    float? TamingProgress,
    Awareness Awareness = Awareness.Calm,
    bool Hostile = false,
    bool? TargetsYou = null,
    float? GrowthProgress = null,
    bool HasGivenName = false);
