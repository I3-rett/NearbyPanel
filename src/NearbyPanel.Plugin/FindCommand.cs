using System;
using System.Collections.Generic;
using NearbyPanel.Core;
using UnityEngine;

namespace NearbyPanel;

/// <summary>
/// The <c>nearby_find</c> console command: which chests within 50 m hold an item whose
/// name contains the text, nearest first, with the panel's own columns, plus a
/// temporary map pin on each so you can walk to it. Also names any saddled animal in
/// range, because a saddle is the one item that lives on a creature rather than in a
/// chest.
///
/// On demand only. The periodic scanner never looks at containers, so the panel's
/// cost is unchanged and no one is listing a base's chests by accident. Contents are
/// read from <c>Container.GetInventory()</c>, which every client keeps in step with the
/// ZDO; nothing is opened, claimed or asked of the owner. Pins are local
/// (<c>save: false</c>) and gone at logout; the command with no argument removes them.
/// </summary>
internal static class FindCommand
{
    private const string Name = "nearby_find";

    /// <summary>The pins this command placed, so a re-run replaces them instead of stacking them.</summary>
    private static readonly List<Minimap.PinData> Pins = new();

    public static void Register(EntityScanner scanner)
    {
        _ = new Terminal.ConsoleCommand(
            Name,
            "lists the chests within " + Tuning.RadiusLabel + " m holding an item whose name contains the text, "
            + "and pins them on the map; no argument removes the pins",
            args => Run(args, scanner));
    }

    private static void Run(Terminal.ConsoleEventArgs args, EntityScanner scanner)
    {
        RemovePins();

        string query = args.Length > 1 ? string.Join(" ", args.Args, 1, args.Length - 1).Trim() : string.Empty;
        if (query.Length == 0)
        {
            ConsoleOutput.Write(args, "nearby_find pins removed; give an item name to search");
            return;
        }

        // Scan() is what fixes Viewer/Forward for this frame; its rows are also the
        // saddle check below.
        IReadOnlyList<NearbyEntity> creatures = scanner.Scan(_ => true, namedFirst: false);
        if (Player.m_localPlayer == null)
        {
            ConsoleOutput.Write(args, "no local player yet");
            return;
        }

        IReadOnlyList<NearbyEntity> chests = NearbyList.Build(
            Containers(query), scanner.Viewer, Tuning.ScanRadius, int.MaxValue);

        string scope = $"\"{query}\" within {Tuning.RadiusLabel} m";
        if (chests.Count == 0)
        {
            ConsoleOutput.Write(args, $"no chest with {scope}");
        }
        else
        {
            ConsoleOutput.Write(args, $"{chests.Count} chest{(chests.Count == 1 ? "" : "s")} with {scope}");
            ConsoleOutput.Write(args, RowFormatter.Header);
            foreach (NearbyEntity chest in chests)
            {
                ConsoleOutput.Write(args, RowFormatter.Row(chest, scanner.Viewer, scanner.Forward, Plugin.DirectionFormat, Plugin.ProgressFormat));
                AddPin(chest);
            }

            ConsoleOutput.Write(args, $"{Pins.Count} pin{(Pins.Count == 1 ? "" : "s")} on the map until you log out; `nearby_find` alone removes them");
        }

        WriteSaddled(args, scanner, creatures, headerShown: chests.Count > 0);
    }

    /// <summary>
    /// Every instantiated container, mapped. One that throws is skipped and counted,
    /// like the scanner's per-creature guard, so one broken chest cannot hide the rest.
    /// </summary>
    private static IEnumerable<NearbyEntity> Containers(string query)
    {
        var rows = new List<NearbyEntity>();
        int skipped = 0;
        string? firstError = null;

        foreach (Container container in UnityEngine.Object.FindObjectsByType<Container>(FindObjectsSortMode.None))
        {
            try
            {
                NearbyEntity? row = EntityMapper.FromContainer(container, query);
                if (row != null)
                {
                    rows.Add(row);
                }
            }
            catch (Exception error)
            {
                skipped++;
                firstError ??= container != null ? container.name + ": " + error.Message : error.Message;
            }
        }

        if (skipped > 0)
        {
            Plugin.Log.LogWarning($"nearby_find: {skipped} container(s) skipped, first: {firstError}");
        }

        return rows;
    }

    private static void WriteSaddled(Terminal.ConsoleEventArgs args, EntityScanner scanner, IReadOnlyList<NearbyEntity> creatures, bool headerShown)
    {
        bool any = false;
        foreach (NearbyEntity creature in creatures)
        {
            if (!creature.Saddled)
            {
                continue;
            }

            if (!any)
            {
                ConsoleOutput.Write(args, "saddled animals in range (the saddle is on them, not in a chest):");
                if (!headerShown)
                {
                    ConsoleOutput.Write(args, RowFormatter.Header);
                }

                any = true;
            }

            ConsoleOutput.Write(args, RowFormatter.Row(creature, scanner.Viewer, scanner.Forward, Plugin.DirectionFormat, Plugin.ProgressFormat));
        }
    }

    private static void AddPin(NearbyEntity chest)
    {
        Minimap map = Minimap.instance;
        if (map == null)
        {
            return;
        }

        // Icon3 is the plain dot. Named after the match so the map says what was found.
        Vector3 position = new(chest.Position.X, chest.Position.Y, chest.Position.Z);
        Pins.Add(map.AddPin(position, Minimap.PinType.Icon3, PinLabel(chest), save: false, isChecked: false));
    }

    /// <summary>A map pin is a marker, not an inventory: the first item names it, the row has the full list.</summary>
    private static string PinLabel(NearbyEntity chest)
    {
        if (string.IsNullOrEmpty(chest.Status))
        {
            return Name;
        }

        int comma = chest.Status!.IndexOf(", ", StringComparison.Ordinal);
        return comma < 0 ? chest.Status : chest.Status.Substring(0, comma);
    }

    private static void RemovePins()
    {
        Minimap map = Minimap.instance;
        if (map != null)
        {
            foreach (Minimap.PinData pin in Pins)
            {
                map.RemovePin(pin);
            }
        }

        Pins.Clear();
    }
}
