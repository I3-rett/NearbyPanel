using System;
using System.Collections.Generic;
using NearbyPanel.Core;
using UnityEngine;

namespace NearbyPanel;

/// <summary>
/// Collects the entities around the local player. Buffers are reused between
/// scans because this runs several times a second.
/// </summary>
internal sealed class EntityScanner
{
    private readonly List<Character> _characters = new();
    private readonly List<NearbyEntity> _found = new();

    /// <summary>Where the player is, valid only after a successful scan.</summary>
    public Vec3 Viewer { get; private set; }

    /// <summary>Where the player faces, valid only after a successful scan.</summary>
    public Vec3 Forward { get; private set; }

    /// <summary>
    /// Everything the last scan read, before the radius, filter and cap — including
    /// the breeding margin beyond the list. For the dump's breeding detail.
    /// </summary>
    public IReadOnlyList<NearbyEntity> Found => _found;

    /// <summary>True when the last scan hit an error and the rows may be stale.</summary>
    public bool Faulted { get; private set; }

    /// <summary>
    /// The rows to show, nearest first. Empty when there is no local player yet —
    /// the main menu, world loading, and the moment of death and respawn all hit
    /// this, so callers must not assume a player exists.
    ///
    /// A creature that throws while being read is skipped rather than being allowed
    /// to abort the whole scan: one bad prefab should not take the panel down, and
    /// an unguarded throw here would repeat four times a second forever.
    /// </summary>
    public IReadOnlyList<NearbyEntity> Scan(Func<NearbyEntity, bool>? include = null, bool namedFirst = false)
    {
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            Faulted = false;
            return Array.Empty<NearbyEntity>();
        }

        Transform transform = player.transform;
        Viewer = EntityMapper.ToVec3(transform.position);
        Forward = EntityMapper.ToVec3(transform.forward);

        _characters.Clear();
        _found.Clear();
        Faulted = false;

        try
        {
            // Cleared first: the game appends to this list rather than replacing it.
            Character.GetCharactersInRange(transform.position, Tuning.ScanRadius, _characters);
        }
        catch (Exception error)
        {
            Fault("range query failed", error);
            return Array.Empty<NearbyEntity>();
        }

        MapAll(transform.position, minimumDistance: 0f);

        // A second, wider pass so an animal near the edge of the list is counted
        // against its whole pen. How much wider comes from the animals themselves:
        // each species carries its own breeding ranges. Only the ring beyond the first
        // pass is read again, and Build still cuts the rows at ScanRadius.
        try
        {
            float reach = BreedingRules.Reach(_found, Viewer, Tuning.ScanRadius);
            if (reach > 0f)
            {
                _characters.Clear();
                Character.GetCharactersInRange(transform.position, Tuning.ScanRadius + reach, _characters);
                MapAll(transform.position, minimumDistance: Tuning.ScanRadius);
            }
        }
        catch (Exception error)
        {
            Fault("breeding range query failed", error);
        }

        try
        {
            MapFish(transform.position);
        }
        catch (Exception error)
        {
            Fault("fish query failed", error);
        }

        try
        {
            BreedingRules.Resolve(_found);
        }
        catch (Exception error)
        {
            Fault("could not work out breeding", error);
        }

        try
        {
            return NearbyList.Build(_found, Viewer, Tuning.ScanRadius, int.MaxValue, include, namedFirst);
        }
        catch (Exception error)
        {
            Fault("could not build the list", error);
            return Array.Empty<NearbyEntity>();
        }
    }

    /// <summary>
    /// Maps every queried character at least <paramref name="minimumDistance"/> away,
    /// with the game's own comparison, so the second pass never re-reads the first.
    /// </summary>
    private void MapAll(Vector3 viewer, float minimumDistance)
    {
        float minimumSqr = minimumDistance * minimumDistance;

        foreach (Character character in _characters)
        {
            try
            {
                if (minimumDistance > 0f && (character.transform.position - viewer).sqrMagnitude < minimumSqr)
                {
                    continue;
                }

                NearbyEntity? entity = EntityMapper.FromCharacter(character);
                if (entity != null)
                {
                    _found.Add(entity);
                }
            }
            catch (Exception error)
            {
                Fault("could not read a creature", error);
            }
        }
    }

    /// <summary>
    /// Maps the fish within scan radius. A fish is not a <see cref="Character"/>, so the
    /// range query above never sees it; the game keeps <c>Fish.Instances</c> itself, in
    /// <c>OnEnable</c>/<c>OnDisable</c>, and this only reads it. There is no widened ring
    /// as for breeding, because a fish has no breeding range. The distance prefilter
    /// keeps a lake full of far fish from being localized four times a second; Build
    /// cuts at the radius again.
    /// </summary>
    private void MapFish(Vector3 viewer)
    {
        float radiusSqr = Tuning.ScanRadius * Tuning.ScanRadius;

        foreach (var instance in Fish.Instances)
        {
            if (instance is not Fish fish || fish == null)
            {
                continue;
            }

            try
            {
                if ((fish.transform.position - viewer).sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                NearbyEntity? entity = EntityMapper.FromFish(fish);
                if (entity != null)
                {
                    _found.Add(entity);
                }
            }
            catch (Exception error)
            {
                Fault("could not read a fish", error);
            }
        }
    }

    private void Fault(string what, Exception error)
    {
        if (Faulted)
        {
            // Already reported this refresh; do not flood the log four times a second.
            return;
        }

        Faulted = true;
        Plugin.Log.LogWarning($"NearbyPanel: {what} ({error.GetType().Name}: {error.Message})");
    }
}
