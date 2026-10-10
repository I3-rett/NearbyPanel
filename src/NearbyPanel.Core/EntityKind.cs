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
}
