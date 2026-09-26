using NearbyPanel.Core;
using UnityEngine;

namespace NearbyPanel;

/// <summary>
/// The boundary between the game and <see cref="NearbyPanel.Core"/>. Everything
/// that knows about Valheim types stops here; everything past it works on
/// <see cref="NearbyEntity"/>. See docs/adr/0004-core-plugin-split.md.
/// </summary>
internal static class EntityMapper
{
    public static Vec3 ToVec3(Vector3 value) => new(value.x, value.y, value.z);

    /// <summary>
    /// Describes <paramref name="character"/> as a row, or returns null when it
    /// should not be listed at all.
    /// </summary>
    /// <param name="tracker">Smooths taming progress between the owner's writes.</param>
    /// <param name="now">A monotonic clock in seconds, normally <c>Time.unscaledTime</c>.</param>
    public static NearbyEntity? FromCharacter(Character character, TamingTracker tracker, float now)
    {
        if (character == null || character.IsDead())
        {
            return null;
        }

        if (character == Player.m_localPlayer)
        {
            return null;
        }

        Tameable tameable = character.GetComponent<Tameable>();

        EntityKind kind = character.IsPlayer()
            ? EntityKind.Player
            : tameable != null ? EntityKind.Tameable : EntityKind.Creature;

        return new NearbyEntity(
            Name: Localize(character.GetHoverName()),
            Kind: kind,
            Position: ToVec3(character.GetCenterPoint()),
            Level: character.GetLevel(),
            Status: tameable != null ? Localize(tameable.GetStatusString()) : null,
            TamingProgress: TamingProgress(character, tameable, tracker, now));
    }

    /// <summary>
    /// How far along taming is, 0..1, or null when there is nothing to report —
    /// already tamed, not tameable, or the record is not readable yet.
    ///
    /// Read from the ZDO rather than the component, because the value is only
    /// written by whichever peer owns the creature. Reading the ZDO is what lets
    /// this work for an animal a friend is taming. The owner writes once every
    /// three seconds, so the raw figure steps; <see cref="TamingTracker"/> fills in
    /// between, but only while taming is actually running.
    /// </summary>
    private static float? TamingProgress(
        Character character,
        Tameable? tameable,
        TamingTracker tracker,
        float now)
    {
        if (tameable == null || tameable.IsTamed())
        {
            return null;
        }

        // m_tamingTime is serialized per prefab, so it must be read off the live
        // component and never hardcoded.
        float total = tameable.m_tamingTime;
        if (total <= 0f)
        {
            return null;
        }

        ZNetView view = tameable.GetComponent<ZNetView>();
        if (view == null || !view.IsValid())
        {
            return null;
        }

        ZDO zdo = view.GetZDO();
        if (zdo == null)
        {
            return null;
        }

        // Defaulting to the full duration means "no key written yet" reads as 0%,
        // which is what the game itself does.
        float remaining = zdo.GetFloat(ZDOVars.s_tameTimeLeft, total);

        return tracker.Progress(
            key: tameable.GetInstanceID(),
            remainingSeconds: remaining,
            totalSeconds: total,
            now: now,
            paused: IsTamingPaused(character, tameable));
    }

    /// <summary>
    /// Whether the owner's countdown is currently stopped. <c>Tameable.TamingUpdate</c>
    /// returns early when the animal is hungry or when its AI is alerted, so these
    /// are the two conditions under which progress does not move.
    /// </summary>
    private static bool IsTamingPaused(Character character, Tameable tameable)
    {
        if (tameable.IsHungry())
        {
            return true;
        }

        BaseAI ai = character.GetBaseAI();
        return ai != null && ai.IsAlerted();
    }

    private static string Localize(string token) =>
        Localization.instance != null ? Localization.instance.Localize(token) : token;
}
