namespace NearbyPanel.Core;

/// <summary>What family a listed entity belongs to. Drives the row layout.</summary>
public enum EntityKind
{
    Creature,
    Tameable,
    Player,

    /// <summary>
    /// An <c>ItemDrop</c> with a <c>Fish</c> component, not a <c>Character</c>; listed
    /// because it hides under water and its hover only works once landed.
    /// </summary>
    Fish,

    /// <summary>
    /// A <c>Container</c> (chest, wagon, ship hold): never in the live list, only in a
    /// <c>nearby_find</c> answer, where STATUS holds the matching items.
    /// </summary>
    Container,

    /// <summary>
    /// An egg: an <c>ItemDrop</c> carrying the game's <c>EggGrow</c>. Listed because the
    /// hover only says warm or cold, while the reason it is cold is the useful part.
    /// </summary>
    Egg,
}
