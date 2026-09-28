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

        foreach (Character character in _characters)
        {
            try
            {
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

        try
        {
            return NearbyList.Build(_found, Viewer, Tuning.ScanRadius, Tuning.MaxRows, include, namedFirst);
        }
        catch (Exception error)
        {
            Fault("could not build the list", error);
            return Array.Empty<NearbyEntity>();
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
