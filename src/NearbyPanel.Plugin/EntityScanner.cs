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
    private readonly TamingTracker _tracker = new();

    /// <summary>Where the player is, valid only after a successful scan.</summary>
    public Vec3 Viewer { get; private set; }

    /// <summary>Where the player faces, valid only after a successful scan.</summary>
    public Vec3 Forward { get; private set; }

    /// <summary>
    /// The rows to show, nearest first. Empty when there is no local player yet —
    /// the main menu, world loading, and the moment of death and respawn all hit
    /// this, so callers must not assume a player exists.
    /// </summary>
    public IReadOnlyList<NearbyEntity> Scan(Func<NearbyEntity, bool>? include = null)
    {
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            return Array.Empty<NearbyEntity>();
        }

        Transform transform = player.transform;
        Viewer = EntityMapper.ToVec3(transform.position);
        Forward = EntityMapper.ToVec3(transform.forward);

        // unscaledTime so the list keeps up while the game is paused or slowed.
        float now = Time.unscaledTime;

        // Cleared first: the game appends to this list rather than replacing it.
        _characters.Clear();
        Character.GetCharactersInRange(transform.position, Tuning.ScanRadius, _characters);

        _found.Clear();
        foreach (Character character in _characters)
        {
            NearbyEntity? entity = EntityMapper.FromCharacter(character, _tracker, now);
            if (entity != null)
            {
                _found.Add(entity);
            }
        }

        return NearbyList.Build(_found, Viewer, Tuning.ScanRadius, Tuning.MaxRows, include);
    }

    /// <summary>
    /// Drops remembered taming state. Called on leaving a world, since instance ids
    /// are only meaningful for as long as the objects live.
    /// </summary>
    public void Reset() => _tracker.Clear();
}
