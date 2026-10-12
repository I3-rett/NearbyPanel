using System;
using System.Collections.Generic;
using System.Globalization;

namespace NearbyPanel.Core;

/// <summary>
/// What the mapper read off an egg, in the game's own terms: the conditions the species
/// requires, whether each is met right now, and the owner's incubation stamp.
/// </summary>
/// <param name="Stack">Eggs in the stack; a stack above one never incubates.</param>
/// <param name="Warm">The owner has stamped a start time: incubation is running.</param>
/// <param name="Progress">0..1 of the incubation time, null when there is no shared clock.</param>
/// <param name="SecondsLeft">Seconds until it hatches, null when unknown.</param>
/// <param name="NeedsFire">The species needs heat (<c>m_requireNearbyFire</c>).</param>
/// <param name="HasFire">A heat area covers the egg right now.</param>
/// <param name="NeedsRoof">The species needs shelter (<c>m_requireUnderRoof</c>).</param>
/// <param name="UnderRoof">Something is over it.</param>
/// <param name="Cover">0..1 cover the game measured.</param>
/// <param name="RequiredCover">0..1 cover the species requires.</param>
public readonly record struct EggReport(
    int Stack,
    bool Warm,
    float? Progress,
    float? SecondsLeft,
    bool NeedsFire,
    bool HasFire,
    bool NeedsRoof,
    bool UnderRoof,
    float Cover,
    float RequiredCover);

/// <summary>The STATUS cell of an egg row.</summary>
public static class Eggs
{
    /// <summary>
    /// <c>stacked ×3</c>, <c>warm · 62% · 18 min</c>, <c>cold: no fire, no roof</c>,
    /// <c>cold: roof 45% &lt; 70%</c>, or plain <c>cold</c> when every condition is met
    /// and the owner simply has not ticked yet. The game's hover stops at warm or cold;
    /// the reason is what a player walks over to find out.
    /// </summary>
    public static string Describe(EggReport report, ProgressFormat format)
    {
        if (report.Stack > 1)
        {
            return "stacked ×" + report.Stack;
        }

        if (report.Warm)
        {
            return WarmText(report, format);
        }

        var reasons = new List<string>(2);
        if (report.NeedsFire && !report.HasFire)
        {
            reasons.Add("no fire");
        }

        if (report.NeedsRoof)
        {
            if (!report.UnderRoof)
            {
                reasons.Add("no roof");
            }
            else if (report.Cover < report.RequiredCover)
            {
                // Truncation is right for a measurement and wrong for a threshold: 0.7f * 100
                // can land on 69.999 on Mono and would read as 69%, so the threshold rounds.
                string required = ((int)Math.Round(report.RequiredCover * 100f)).ToString(CultureInfo.InvariantCulture) + "%";
                reasons.Add("roof " + RowFormatter.Percent(report.Cover) + " < " + required);
            }
        }

        return reasons.Count == 0 ? "cold" : "cold: " + string.Join(", ", reasons);
    }

    private static string WarmText(EggReport report, ProgressFormat format)
    {
        if (report.Progress is not { } progress)
        {
            return "warm";
        }

        string percent = RowFormatter.Percent(progress > 1f ? 1f : progress);
        string? time = report.SecondsLeft is { } left ? RowFormatter.Duration(left < 0f ? 0f : left) : null;

        return format switch
        {
            ProgressFormat.Time when time != null => "warm · hatches in " + time,
            ProgressFormat.Both when time != null => "warm · " + percent + " · " + time,
            _ => "warm · " + percent,
        };
    }
}
