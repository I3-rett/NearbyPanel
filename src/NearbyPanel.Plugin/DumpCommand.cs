using System.Collections.Generic;
using NearbyPanel.Core;

namespace NearbyPanel;

/// <summary>
/// The <c>nearby_dump</c> console command. Writes the current list to both the
/// console and <c>LogOutput.log</c>, using the same formatter the panel uses, so
/// the log is a faithful record of what the panel showed.
///
/// It applies whatever filter the panel is applying — there is no argument that
/// widens the view beyond what is already on screen. Set the filter with
/// <c>nearby_filter</c>.
/// </summary>
internal static class DumpCommand
{
    private const string Name = "nearby_dump";

    /// <summary>
    /// Registers the command. Constructing a <see cref="Terminal.ConsoleCommand"/>
    /// is what registers it, so the return value is deliberately unused. Re-running
    /// this (a hot reload) replaces the entry rather than throwing, because the
    /// game assigns into its table by indexer.
    /// </summary>
    public static void Register(EntityScanner scanner)
    {
        _ = new Terminal.ConsoleCommand(
            Name,
            "lists the creatures near you, nearest first",
            args => Run(args, scanner));
    }

    private static void Run(Terminal.ConsoleEventArgs args, EntityScanner scanner)
    {
        IReadOnlyList<NearbyEntity> rows = scanner.Scan(FilterState.Predicate(), Plugin.NamedFirst);
        string scope = $"{Tuning.ScanRadius:0} m{FilterState.Describe()}";

        if (rows.Count == 0)
        {
            Write(args, Player.m_localPlayer == null
                ? "no local player yet"
                : $"nothing within {scope}");
            return;
        }

        Write(args, $"{rows.Count} within {scope}");
        Write(args, RowFormatter.Header);

        foreach (NearbyEntity entity in rows)
        {
            Write(args, RowFormatter.Row(entity, scanner.Viewer, scanner.Forward, Plugin.DirectionFormat));
        }
    }

    private static void Write(Terminal.ConsoleEventArgs args, string line)
    {
        // Terminal is a MonoBehaviour, so this needs Unity's null check rather than
        // ?., which would happily call into a destroyed object.
        if (args.Context != null)
        {
            args.Context.AddString(line);
        }

        Plugin.Log.LogInfo(line);
    }
}
