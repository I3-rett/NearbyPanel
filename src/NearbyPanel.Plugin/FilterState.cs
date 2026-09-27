using System;
using BepInEx.Configuration;

namespace NearbyPanel;

/// <summary>
/// The live text filter. Held here rather than read straight from the config entry
/// so the console command and the config file are the same setting: typing
/// <c>nearby_filter boar</c> writes through to the config, and editing the config
/// in an in-game manager is picked up immediately.
///
/// There is no text box in the panel on purpose — it takes no clicks and never
/// grabs keyboard focus (ADR 0005), so the filter is set from the console or the
/// config instead.
/// </summary>
internal static class FilterState
{
    private static ConfigEntry<string>? _entry;

    /// <summary>The current filter, or an empty string for "show everything".</summary>
    public static string Text => _entry?.Value ?? string.Empty;

    public static bool Active => !string.IsNullOrWhiteSpace(Text);

    public static void Bind(ConfigEntry<string> entry) => _entry = entry;

    /// <summary>Sets the filter and persists it. An empty value clears it.</summary>
    public static void Set(string? text)
    {
        if (_entry == null)
        {
            return;
        }

        _entry.Value = string.IsNullOrWhiteSpace(text) ? string.Empty : text!.Trim();
    }

    /// <summary>A short description for the panel title.</summary>
    public static string Describe() => Active ? " matching \"" + Text + "\"" : string.Empty;

    /// <summary>The predicate for the current filter.</summary>
    public static Func<Core.NearbyEntity, bool> Predicate() => Core.NearbyList.Filter(Text);
}
