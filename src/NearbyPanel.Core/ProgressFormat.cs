namespace NearbyPanel.Core;

/// <summary>How the STATUS column expresses taming and growth progress.</summary>
public enum ProgressFormat
{
    /// <summary>How far along: <c>42%</c>, <c>62% grown</c>.</summary>
    Percent,

    /// <summary>
    /// How long is left: <c>4 min fed</c>, <c>grown in 12 min</c>. Taming time only
    /// counts down while the animal is fed, hence the word.
    /// </summary>
    Time,

    /// <summary>Both: <c>42% · 4 min fed</c>, <c>62% grown · 12 min</c>.</summary>
    Both,
}
