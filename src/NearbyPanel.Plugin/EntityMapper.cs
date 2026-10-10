using System;
using System.Collections.Generic;
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
    // Reused: filled once per animal being tamed, four times a second.
    private static readonly List<Player> NearbyPlayers = new();

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
        (float? taming, float? tamingLeft) = Taming(tameable);
        (float? growth, float? growthLeft) = Growth(character);
        Procreation procreation = character.GetComponent<Procreation>();

        return new NearbyEntity(
            Name: RichText.Strip(given ?? Localize(character.m_name)),
            Kind: kind,
            // transform.position, not GetCenterPoint(): the viewer is measured at the
            // player's feet, and comparing feet against a collider centre made the
            // altitude column read about +1 for a creature standing level with you.
            Position: ToVec3(character.transform.position),
            Level: character.GetLevel(),
            Status: tameable != null ? RichText.Strip(Localize(tameable.GetStatusString())) : null,
            TamingProgress: taming,
            Awareness: AwarenessOf(ai),
            Hostile: IsHostileToPlayer(ai),
            TargetsYou: TargetsLocalPlayer(character, ai),
            GrowthProgress: growth,
            HasGivenName: given != null,
            TamingSecondsLeft: tamingLeft,
            GrowthSecondsLeft: growthLeft,
            // The name the game compares when it counts a pen: prefab name + "(Clone)".
            Prefab: character.gameObject.name,
            // The game counts a partner without a Procreation component as ready.
            ReadyToMate: procreation == null || procreation.ReadyForProcreation(),
            Breeding: BreedingOf(procreation, tameable));
    }

    /// <summary>
    /// Describes <paramref name="fish"/> as a row. A fish is not a <see cref="Character"/>:
    /// it is a bare component on an <c>ItemDrop</c> prefab with no AI, no health and no
    /// network record of its own worth reading, so it has its own mapper.
    /// Returns null for a destroyed fish.
    /// </summary>
    public static NearbyEntity? FromFish(Fish fish)
    {
        if (fish == null)
        {
            return null;
        }

        // Fish.m_itemDrop is private, so ask the object for the component instead.
        ItemDrop? drop = fish.GetComponent<ItemDrop>();

        return new NearbyEntity(
            Name: RichText.Strip(Localize(fish.m_name)),
            Kind: EntityKind.Fish,
            Position: ToVec3(fish.transform.position),
            // The game's fish size, 1-3, stored as item quality. The star column subtracts
            // one as it does for creature levels, so "[2]" in the hover is one star here.
            Level: Math.Max(1, drop?.m_itemData?.m_quality ?? 1),
            Status: Baits.Describe(BaitsOf(fish)),
            TamingProgress: null,
            Awareness: Awareness.Calm,
            // Known, not unknown: a fish has no AI and never hunts anyone.
            Hostile: false,
            TargetsYou: false,
            Prefab: fish.gameObject.name);
    }

    /// <summary>The baits a fish takes, named as the player sees them in the inventory.</summary>
    private static IEnumerable<Bait> BaitsOf(Fish fish)
    {
        if (fish.m_baits == null)
        {
            yield break;
        }

        foreach (Fish.BaitSetting setting in fish.m_baits)
        {
            if (setting == null || setting.m_bait == null)
            {
                continue;
            }

            yield return new Bait(
                RichText.Strip(LocalizeOrEmpty(setting.m_bait.m_itemData?.m_shared?.m_name)),
                setting.m_chance);
        }
    }

    /// <summary>
    /// What <c>Procreation.Procreate</c> works from, for a tamed adult that breeds;
    /// null for anything else, since the game only runs it once an animal is tamed.
    /// Love points and the pregnancy stamp are read from the record, so this holds for
    /// animals another peer simulates. Crowding and the partner check are stored
    /// nowhere; <see cref="BreedingRules.Resolve"/> recomputes them afterwards.
    /// </summary>
    private static Breeding? BreedingOf(Procreation? procreation, Tameable? tameable)
    {
        if (procreation == null || tameable == null || !tameable.IsTamed() || procreation.m_offspring == null)
        {
            return null;
        }

        ZDO? zdo = ZdoOf(procreation);
        if (zdo == null)
        {
            return null;
        }

        // Zero is the game's own "not pregnant"; anything else is the tick it began.
        long since = zdo.GetLong(ZDOVars.s_pregnant, 0L);
        bool pregnant = since != 0L;
        float? left = null;

        if (pregnant && ZNet.instance != null && since > 0L && since <= DateTime.MaxValue.Ticks)
        {
            double elapsed = (ZNet.instance.GetTime() - new DateTime(since)).TotalSeconds;
            if (!double.IsNaN(elapsed))
            {
                left = (float)(procreation.m_pregnancyDuration - elapsed);
            }
        }

        GameObject? partner = procreation.m_seperatePartner;

        return new Breeding(
            LovePoints: zdo.GetInt(ZDOVars.s_lovePoints, 0),
            RequiredLovePoints: procreation.m_requiredLovePoints,
            Pregnant: pregnant,
            PregnancySecondsLeft: left,
            PregnancyDuration: procreation.m_pregnancyDuration,
            OffspringPrefab: procreation.m_offspring.name + "(Clone)",
            PartnerPrefab: partner != null ? partner.name + "(Clone)" : procreation.gameObject.name,
            SeparatePartner: partner != null,
            NeedsPartner: procreation.m_noPartnerOffspring == null,
            CrowdRange: procreation.m_totalCheckRange,
            MaxCrowd: procreation.m_maxCreatures,
            PartnerRange: procreation.m_partnerCheckRange,
            Hungry: tameable.IsHungry());
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
    /// How far along taming is, 0..1, and the feeding seconds still owed, or nulls
    /// when there is nothing to report — already tamed, not tameable, or the record
    /// is not readable yet. The seconds are <c>s_tameTimeLeft</c> itself.
    ///
    /// Read from the ZDO rather than the component, because the value is only
    /// written by whichever peer owns the creature. That is what lets this work for
    /// an animal a friend is taming. The owner writes once every three seconds and
    /// vanilla taming takes many minutes, so the whole-percent figure simply steps
    /// every few seconds; it is reported as read, with no smoothing.
    /// </summary>
    private static (float? Progress, float? SecondsLeft) Taming(Tameable? tameable)
    {
        if (tameable == null || tameable.IsTamed())
        {
            return (null, null);
        }

        // m_tamingTime is serialized per prefab, so it must be read off the live
        // component and never hardcoded.
        float total = tameable.m_tamingTime;
        if (total <= 0f)
        {
            return (null, null);
        }

        ZDO? zdo = ZdoOf(tameable);
        if (zdo == null)
        {
            return (null, null);
        }

        // Defaulting to the full duration means "no key written yet" reads as 0%,
        // which is what the game itself does.
        float remaining = Mathf.Clamp(zdo.GetFloat(ZDOVars.s_tameTimeLeft, total), 0f, total);

        // The stored seconds are unboosted: the game speeds up each tick instead, so
        // the percentage stays as stored and only the time left is divided.
        float left = TamingBoost.SecondsLeft(
            remaining, BoostedPlayers(tameable), tameable.m_tamingBoostMultiplier);

        return (1f - (remaining / total), left);
    }

    /// <summary>
    /// Players within the animal's boost range carrying the TamingBoost attribute —
    /// Brew of Animal Whispers — counted as <c>Tameable.DecreaseRemainingTime</c> does,
    /// this player included. A friend's brew counts too: for a player this client does
    /// not own, the attribute is read from the replicated record.
    /// </summary>
    private static int BoostedPlayers(Tameable tameable)
    {
        NearbyPlayers.Clear();
        Player.GetPlayersInRange(
            tameable.transform.position, tameable.m_tamingSpeedMultiplierRange, NearbyPlayers);

        int boosted = 0;
        foreach (Player player in NearbyPlayers)
        {
            if (player.GetSEMan().HaveStatusAttribute(StatusEffect.StatusAttribute.TamingBoost))
            {
                boosted++;
            }
        }

        return boosted;
    }

    /// <summary>
    /// How far a young animal is towards its adult form, 0..1, and the seconds until
    /// it gets there, or nulls when there is nothing to report — no growth stage, or
    /// no birth time in the record yet.
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
    private static (float? Progress, float? SecondsLeft) Growth(Character character)
    {
        Growup growup = character.GetComponent<Growup>();
        if (growup == null)
        {
            return (null, null);
        }

        // m_growTime is serialized per prefab, so it must be read off the live
        // component and never hardcoded.
        float total = growup.m_growTime;
        if (total <= 0f)
        {
            return (null, null);
        }

        ZDO? zdo = ZdoOf(growup);
        if (zdo == null || ZNet.instance == null)
        {
            return (null, null);
        }

        // Zero means no peer has stamped a birth time yet, which is unknown rather
        // than "just born": the game writes it in BaseAI.Awake, and only once, so a
        // value we can read is a value that will not move under us.
        long born = zdo.GetLong(ZDOVars.s_spawnTime, 0L);
        if (born <= 0L || born > DateTime.MaxValue.Ticks)
        {
            return (null, null);
        }

        double elapsed = (ZNet.instance.GetTime() - new DateTime(born)).TotalSeconds;
        if (double.IsNaN(elapsed))
        {
            return (null, null);
        }

        float progress = Mathf.Clamp01((float)(elapsed / total));
        return (progress, total * (1f - progress));
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

    // A modded prefab may lack a name; an empty one is left out of the bait text.
    private static string LocalizeOrEmpty(string? token) =>
        token == null ? string.Empty : Localize(token);

    private static string Localize(string token) =>
        Localization.instance != null ? Localization.instance.Localize(token) : token;
}
