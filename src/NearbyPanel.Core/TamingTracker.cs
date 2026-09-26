using System.Collections.Generic;

namespace NearbyPanel.Core;

/// <summary>
/// Smooths taming progress between the owner's writes.
///
/// The peer that owns a creature writes the remaining taming time once every three
/// seconds. Read raw, a progress figure therefore sits still and then jumps, which
/// looks broken. Between writes the countdown runs at one second per second, so the
/// value in between can be predicted — but only while taming is actually running.
/// A hungry or frightened animal makes no progress at all, and predicting through
/// that would show a bar climbing on an animal that has stopped.
///
/// So: remember the last value seen for each creature, and extrapolate from it only
/// while unpaused, and only as far as the next write is expected. Anything beyond
/// that is guesswork and is held instead.
/// </summary>
public sealed class TamingTracker
{
    /// <summary>The owner's write interval, from <c>Tameable.Awake</c>.</summary>
    public const float OwnerWriteIntervalSeconds = 3f;

    /// <summary>Entries untouched for this long are dropped.</summary>
    private const float ForgetAfterSeconds = 60f;

    private readonly Dictionary<int, Entry> _entries = new();
    private float _lastPrune;

    /// <summary>
    /// Progress in 0..1 for the creature identified by <paramref name="key"/>.
    /// </summary>
    /// <param name="remainingSeconds">The value currently in the record.</param>
    /// <param name="totalSeconds">The creature's full taming duration.</param>
    /// <param name="now">A monotonically increasing clock, in seconds.</param>
    /// <param name="paused">True when the animal is hungry or frightened.</param>
    public float Progress(int key, float remainingSeconds, float totalSeconds, float now, bool paused)
    {
        if (totalSeconds <= 0f)
        {
            return 0f;
        }

        Prune(now);

        if (!_entries.TryGetValue(key, out Entry entry) || entry.Remaining != remainingSeconds)
        {
            // First sight, or the owner just wrote a new value: trust it as-is.
            entry = new Entry(remainingSeconds, now);
            _entries[key] = entry;
        }
        else
        {
            entry = new Entry(entry.Remaining, entry.SeenAt, now);
            _entries[key] = entry;
        }

        float effective = remainingSeconds;
        if (!paused)
        {
            float elapsed = now - entry.SeenAt;
            if (elapsed > 0f)
            {
                // Never predict further than the next write is due, so a stalled
                // owner cannot make the bar run away.
                effective -= elapsed < OwnerWriteIntervalSeconds ? elapsed : OwnerWriteIntervalSeconds;
            }
        }

        if (effective < 0f)
        {
            effective = 0f;
        }

        float progress = 1f - (effective / totalSeconds);
        return progress < 0f ? 0f : progress > 1f ? 1f : progress;
    }

    /// <summary>Drops everything, for a scene change or a disconnect.</summary>
    public void Clear()
    {
        _entries.Clear();
        _lastPrune = 0f;
    }

    /// <summary>How many creatures are currently remembered. For tests.</summary>
    public int Count => _entries.Count;

    private void Prune(float now)
    {
        if (now - _lastPrune < ForgetAfterSeconds)
        {
            return;
        }

        _lastPrune = now;

        List<int>? stale = null;
        foreach (KeyValuePair<int, Entry> pair in _entries)
        {
            if (now - pair.Value.TouchedAt > ForgetAfterSeconds)
            {
                stale ??= new List<int>();
                stale.Add(pair.Key);
            }
        }

        if (stale == null)
        {
            return;
        }

        foreach (int key in stale)
        {
            _entries.Remove(key);
        }
    }

    private readonly struct Entry
    {
        public Entry(float remaining, float seenAt)
            : this(remaining, seenAt, seenAt)
        {
        }

        public Entry(float remaining, float seenAt, float touchedAt)
        {
            Remaining = remaining;
            SeenAt = seenAt;
            TouchedAt = touchedAt;
        }

        /// <summary>The value the owner last wrote.</summary>
        public float Remaining { get; }

        /// <summary>When that value was first seen.</summary>
        public float SeenAt { get; }

        /// <summary>When this entry was last asked about, for pruning.</summary>
        public float TouchedAt { get; }
    }
}
