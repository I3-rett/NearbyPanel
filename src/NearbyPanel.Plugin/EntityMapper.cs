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
    public static NearbyEntity? FromCharacter(Character character)
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

        BaseAI ai = character.GetBaseAI();

        return new NearbyEntity(
            Name: NameOf(character, tameable),
            Kind: kind,
            // transform.position, not GetCenterPoint(): the viewer is measured at the
            // player's feet, and comparing feet against a collider centre made the
            // altitude column read about +1 for a creature standing level with you.
            Position: ToVec3(character.transform.position),
            Level: character.GetLevel(),
            Status: tameable != null ? Localize(tameable.GetStatusString()) : null,
            TamingProgress: TamingProgress(tameable),
            Awareness: AwarenessOf(ai),
            Hostile: IsHostileToPlayer(ai),
            TargetsYou: TargetsLocalPlayer(character, ai));
    }

    /// <summary>
    /// What to call this creature: its given name if it has been named, otherwise
    /// its species.
    ///
    /// The name is read out of the network record directly rather than through
    /// <c>Character.GetHoverName()</c>, which for a tamed animal reaches
    /// <c>Tameable.GetText()</c> and can write back a legacy author id — a write the
    /// game guards behind a dedicated-server check, but a write all the same. This
    /// mod only reads. It also avoids localizing an already-localized string, which
    /// pollutes the game's translation cache with identity entries.
    /// </summary>
    private static string NameOf(Character character, Tameable? tameable)
    {
        if (tameable != null && tameable.IsTamed())
        {
            ZDO? zdo = ZdoOf(tameable);
            if (zdo != null)
            {
                string given = zdo.GetString(ZDOVars.s_tamedName, string.Empty);
                if (!string.IsNullOrEmpty(given))
                {
                    return given;
                }
            }
        }

        return Localize(character.m_name);
    }

    /// <summary>
    /// How far along taming is, 0..1, or null when there is nothing to report —
    /// already tamed, not tameable, or the record is not readable yet.
    ///
    /// Read from the ZDO rather than the component, because the value is only
    /// written by whichever peer owns the creature. That is what lets this work for
    /// an animal a friend is taming. The owner writes once every three seconds and
    /// vanilla taming takes many minutes, so the whole-percent figure simply steps
    /// every few seconds; it is reported as read, with no smoothing.
    /// </summary>
    private static float? TamingProgress(Tameable? tameable)
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

        ZDO? zdo = ZdoOf(tameable);
        if (zdo == null)
        {
            return null;
        }

        // Defaulting to the full duration means "no key written yet" reads as 0%,
        // which is what the game itself does.
        float remaining = zdo.GetFloat(ZDOVars.s_tameTimeLeft, total);
        return 1f - Mathf.Clamp01(remaining / total);
    }

    /// <summary>
    /// How awake this creature is. Both flags come from the network record —
    /// <c>IsAlerted</c> through the alert flag a non-owner is given in
    /// <c>BaseAI.UpdateAI</c>, <c>HaveTarget</c> read straight out of the record —
    /// so this is meaningful for any creature in range, not only ones we simulate.
    /// </summary>
    private static Awareness AwarenessOf(BaseAI? ai)
    {
        if (ai == null)
        {
            return Awareness.Calm;
        }

        if (ai.IsAlerted())
        {
            return Awareness.Alerted;
        }

        return ai.HaveTarget() ? Awareness.Tracking : Awareness.Calm;
    }

    /// <summary>
    /// Whether this creature would attack the local player. Computed locally from
    /// factions, groups, taming and aggravation, so it needs no replication.
    /// </summary>
    private static bool IsHostileToPlayer(BaseAI? ai)
    {
        Player player = Player.m_localPlayer;
        return ai != null && player != null && ai.IsEnemy(player);
    }

    /// <summary>
    /// Whether this creature is targeting the local player specifically.
    ///
    /// Returns null when the answer cannot be known. A creature's target lives in a
    /// private field on the peer simulating it and is not replicated, so for
    /// anything owned by the server or another player there is nothing to read.
    /// Null is deliberately not false: reporting "not hunting you" when the truth is
    /// unknown is the one error here that could get someone killed.
    /// </summary>
    private static bool? TargetsLocalPlayer(Character character, BaseAI? ai)
    {
        if (ai == null)
        {
            return null;
        }

        ZNetView view = character.GetComponent<ZNetView>();
        if (view == null || !view.IsValid() || !view.IsOwner())
        {
            return null;
        }

        Player player = Player.m_localPlayer;
        return player != null && ai.GetTargetCreature() == player;
    }

    private static ZDO? ZdoOf(Component component)
    {
        ZNetView view = component.GetComponent<ZNetView>();
        return view != null && view.IsValid() ? view.GetZDO() : null;
    }

    private static string Localize(string token) =>
        Localization.instance != null ? Localization.instance.Localize(token) : token;
}
