using System.Collections.Generic;

namespace NearbyPanel.Core;

/// <summary>
/// What a tamed adult's <c>Procreation</c> component would decide, stripped of game
/// types. The adapter fills everything the game stores or serializes;
/// <see cref="Crowd"/> and <see cref="Partners"/> are stored nowhere and are
/// recomputed by <see cref="BreedingRules.Resolve"/> from the other creatures.
/// </summary>
/// <param name="Pregnant">Whether a pregnancy is stamped in the record.</param>
/// <param name="PregnancySecondsLeft">Until due, or null when the clock cannot say.</param>
/// <param name="OffspringPrefab">Instance name of the young, which crowd the pen too.</param>
/// <param name="PartnerPrefab">Instance name of what counts as a partner: its own kind, or a separate one.</param>
/// <param name="SeparatePartner">One partner of another kind suffices, rather than two of its own (self included).</param>
/// <param name="NeedsPartner">False when the game lets it breed alone.</param>
/// <param name="Hungry">The game skips love points while hungry, before any other check.</param>
/// <param name="Crowd">Own kind plus offspring within <paramref name="CrowdRange"/>, self included.</param>
/// <param name="Partners">Ready partners within <paramref name="PartnerRange"/>, self included when its own kind.</param>
/// <param name="NearestCrowder">
/// Name of the nearest animal counted in <paramref name="Crowd"/> that is not itself and
/// not a partner it needs — the one to move first. Null when there is none.
/// </param>
/// <param name="NearestCrowderDistance">Its 3-D distance, in metres.</param>
public sealed record Breeding(
    int LovePoints,
    int RequiredLovePoints,
    bool Pregnant,
    float? PregnancySecondsLeft,
    float PregnancyDuration,
    string OffspringPrefab,
    string PartnerPrefab,
    bool SeparatePartner,
    bool NeedsPartner,
    float CrowdRange,
    int MaxCrowd,
    float PartnerRange,
    bool Hungry,
    int? Crowd = null,
    int? Partners = null,
    string? NearestCrowder = null,
    float? NearestCrowderDistance = null)
{
    /// <summary>0..1 of the way to term, or null when not pregnant or unknown.</summary>
    public float? PregnancyProgress =>
        Pregnant && PregnancySecondsLeft is { } left && PregnancyDuration > 0f
            ? 1f - (left < 0f ? 0f : left > PregnancyDuration ? PregnancyDuration : left) / PregnancyDuration
            : null;

    /// <summary>Whether too many of its kind and its young stand within reach.</summary>
    public bool IsCrowded => Crowd is { } crowd && crowd >= MaxCrowd;

    /// <summary>Whether the partner check fails, as far as it is known.</summary>
    public bool LacksPartner =>
        NeedsPartner && Partners is { } partners && partners < (SeparatePartner ? 1 : 2);
}

/// <summary>
/// Recomputes the two breeding checks the game never stores, with the rule of
/// <c>SpawnSystem.GetNrOfInstances</c>: same instance name, full 3-D distance, and
/// inclusive at the edge — the game skips only what is further than the range. A
/// 3-D sphere is why parents kept on a platform above their young go on breeding.
/// </summary>
public static class BreedingRules
{
    /// <summary>
    /// How far beyond the list the scan must look so every listed breeding animal is
    /// counted against its whole pen: the widest crowding or partner range among those
    /// within <paramref name="radius"/>. Ranges are serialized per species — 20 m for a
    /// lox — so this is read from the animals, never assumed. Zero when none breeds.
    /// </summary>
    public static float Reach(IEnumerable<NearbyEntity> found, Vec3 viewer, float radius)
    {
        float reach = 0f;

        foreach (NearbyEntity entity in found)
        {
            if (entity.Breeding is not { } breeding || Geometry.Distance(viewer, entity.Position) > radius)
            {
                continue;
            }

            if (breeding.CrowdRange > reach)
            {
                reach = breeding.CrowdRange;
            }

            if (breeding.PartnerRange > reach)
            {
                reach = breeding.PartnerRange;
            }
        }

        return reach;
    }

    /// <summary>
    /// Fills <see cref="Breeding.Crowd"/> and <see cref="Breeding.Partners"/> for every
    /// creature in <paramref name="found"/> that breeds, counting against all of
    /// <paramref name="found"/>. In place: the scanner reuses its list every refresh.
    /// </summary>
    public static void Resolve(IList<NearbyEntity> found)
    {
        for (int i = 0; i < found.Count; i++)
        {
            NearbyEntity self = found[i];
            if (self.Breeding is not { } breeding || self.Prefab == null)
            {
                continue;
            }

            int crowd = 0;
            int partners = 0;
            NearbyEntity? nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (NearbyEntity other in found)
            {
                float distance = Geometry.Distance(self.Position, other.Position);

                bool partner = distance <= breeding.PartnerRange
                    && other.Prefab == breeding.PartnerPrefab
                    && other.ReadyToMate;

                if (partner)
                {
                    partners++;
                }

                if (distance <= breeding.CrowdRange
                    && (other.Prefab == self.Prefab || other.Prefab == breeding.OffspringPrefab))
                {
                    crowd++;

                    // Moving a partner it needs would trade Crowded for No partner.
                    if (!ReferenceEquals(other, self) && !partner && distance < nearestDistance)
                    {
                        nearest = other;
                        nearestDistance = distance;
                    }
                }
            }

            found[i] = self with
            {
                Breeding = breeding with
                {
                    Crowd = crowd,
                    Partners = partners,
                    NearestCrowder = nearest?.Name,
                    NearestCrowderDistance = nearest != null ? nearestDistance : null,
                },
            };
        }
    }
}
