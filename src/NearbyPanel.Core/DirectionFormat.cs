namespace NearbyPanel.Core;

/// <summary>How the DIR column expresses where something is.</summary>
public enum DirectionFormat
{
    /// <summary>
    /// Degrees from where you are looking: 0 ahead, 90 to your right, -90 to your
    /// left, 180 behind. Turns as you turn.
    /// </summary>
    Degrees,

    /// <summary>
    /// Relative letters: F ahead, R right, B behind, L left, and the diagonals.
    /// The same information as <see cref="Degrees"/>, coarser and quicker to read.
    /// </summary>
    Relative,

    /// <summary>
    /// World compass points, N NE E SE S SW W NW, independent of which way you
    /// face. Matches the map rather than the screen.
    /// </summary>
    Compass,
}
