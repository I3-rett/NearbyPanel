using System;

namespace NearbyPanel.Core;

/// <summary>
/// The taming speed-up from players carrying the TamingBoost attribute — Brew of
/// Animal Whispers in vanilla. The game stores taming time left unboosted and speeds
/// up each tick instead, so this mirrors that to turn the stored figure into the time
/// actually left. An estimate: the brew wears off, and players come and go.
/// </summary>
public static class TamingBoost
{
    /// <summary>
    /// <paramref name="remaining"/> seconds divided by <paramref name="multiplier"/>
    /// once per boosted player, as <c>Tameable.DecreaseRemainingTime</c> multiplies
    /// each tick. A multiplier that could not speed taming up is ignored.
    /// </summary>
    public static float SecondsLeft(float remaining, int boostedPlayers, float multiplier)
    {
        if (boostedPlayers <= 0 || !(multiplier > 0f))
        {
            return remaining;
        }

        return (float)(remaining / Math.Pow(multiplier, boostedPlayers));
    }
}
