namespace NearbyPanel.Core;

/// <summary>
/// How awake a creature is, as the game itself tracks it. All three states come
/// from network-replicated flags, so they are readable for any creature in range,
/// not only the ones this client happens to simulate.
/// </summary>
public enum Awareness
{
    /// <summary>Has noticed nothing. The state to sneak up on.</summary>
    Calm,

    /// <summary>
    /// Has a target but is not alerted — the game's own "?" marker. It is aware
    /// that something is about, without having committed to it.
    /// </summary>
    Tracking,

    /// <summary>
    /// Alerted: the game's "!" marker. Note this does not say alerted *at you* —
    /// it could be reacting to another player, a tamed animal, or another creature.
    /// Only <see cref="NearbyEntity.TargetsYou"/> can say that, and only sometimes.
    /// </summary>
    Alerted,
}
