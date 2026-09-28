using System;
using NearbyPanel.Core;
using UnityEngine;

namespace NearbyPanel;

/// <summary>
/// The boundary between the game and <see cref="NearbyPanel.Core"/>. Everything
/// that knows about Valheim types stops here; everything past it works on
/// <see cref="NearbyEntity"/>, which can be tested without a running game.
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
        string? given = GivenName(tameable);

        return new NearbyEntity(
            Name: given ?? Localize(character.m_name),
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
            TargetsYou: TargetsLocalPlayer(character, ai),
            GrowthProgress: GrowthProgress(character),
            HasGivenName: given != null);
    }

    /// <summary>
    /// The name a player gave this creature, or null when it has none — in which
    /// case the row shows the species, and the creature does not count as named.
    ///
    /// The name is read out of the network record directly rather than through
    /// <c>Character.GetHoverName()</c>, which for a tamed animal reaches
    /// <c>Tameable.GetText()</c> and can write back a legacy author id — a write the
    /// game guards behind a dedicated-server check, but a write all the same. This
    /// mod only reads. It also avoids localizing an already-localized string, which
    /// pollutes the game's translation cache with identity entries.
    /// </summary>
    private static string? GivenName(Tameable? tameable)
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

        return null;
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
    /// How far a young animal is towards its adult form, 0..1, or null when there is
    /// nothing to report — no growth stage, or no birth time in the record yet.
    ///
    /// <c>Growup.GrowUpdate</c> does nothing but compare the time since the creature
    /// spawned against <c>m_growTime</c>, so the same two numbers give the progress.
    /// Both are readable without owning the creature: the birth instant is a long in
    /// the network record, and the clock is network time, shared by everyone.
    ///
    /// Deliberately not via <c>BaseAI.GetTimeSinceSpawned()</c>. That method writes:
    /// when the key is unset it stamps the current time into the record and returns
    /// zero. Calling it would make this mod author world state, and would reset the
    /// growth of any creature whose birth time had not yet been recorded. The same
    /// trap as <c>Character.GetHoverName()</c> — read the record instead.
    /// </summary>
    private static float? GrowthProgress(Character character)
    {
        Growup growup = character.GetComponent<Growup>();
        if (growup == null)
        {
            return null;
        }

        // m_growTime is serialized per prefab, so it must be read off the live
        // component and never hardcoded.
        float total = growup.m_growTime;
        if (total <= 0f)
        {
            return null;
        }

        ZDO? zdo = ZdoOf(growup);
        if (zdo == null || ZNet.instance == null)
        {
            return null;
        }

        // Zero means no peer has stamped a birth time yet, which is unknown rather
        // than "just born": the game writes it in BaseAI.Awake, and only once, so a
        // value we can read is a value that will not move under us.
        long born = zdo.GetLong(ZDOVars.s_spawnTime, 0L);
        if (born <= 0L || born > DateTime.MaxValue.Ticks)
        {
            return null;
        }

        double elapsed = (ZNet.instance.GetTime() - new DateTime(born)).TotalSeconds;
        if (double.IsNaN(elapsed))
        {
            return null;
        }

        return Mathf.Clamp01((float)(elapsed / total));
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
    ///
    /// In practice this resolves for most creatures on a dedicated server too: a
    /// client generally owns the ones loaded around it. That was an open question
    /// when this was written - the column looked like it might stay silent in
    /// multiplayer - and it turned out unfounded. It is not a guarantee, only the
    /// common case, which is exactly why null still means unknown.
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
