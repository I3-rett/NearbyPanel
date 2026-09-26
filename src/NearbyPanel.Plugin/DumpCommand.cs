using System.Collections.Generic;
using NearbyPanel.Core;

namespace NearbyPanel;

/// <summary>
/// The <c>nearby_dump</c> console command. Writes the current list to both the
/// console and <c>LogOutput.log</c>, using the same formatter the panel will use,
/// so the log is a faithful record of what the panel showed.
///
/// This exists before any UI on purpose: it makes the data layer observable, and
/// it gives a diffable artefact to compare against the game's own hover text.
/// </summary>
internal static class DumpCommand
{
    private const string Name = "nearby_dump";

    /// <summary>
    /// Registers the command. Constructing a <see cref="Terminal.ConsoleCommand"/>
    /// is what registers it, so the return value is deliberately unused.
    /// </summary>
    public static void Register(EntityScanner scanner)
    {
        _ = new Terminal.ConsoleCommand(
            Name,
            "lists the entities near you, nearest first",
            args => Run(args, scanner));
    }

    private static void Run(Terminal.ConsoleEventArgs args, EntityScanner scanner)
    {
        bool all = args.Length > 1 && args[1] == "all";

        IReadOnlyList<NearbyEntity> rows = all
            ? scanner.Scan()
            : scanner.Scan(NearbyList.IsTameable);

        if (rows.Count == 0)
        {
            Write(args, Player.m_localPlayer == null
                ? "no local player yet"
                : $"nothing within {Tuning.ScanRadius:0} m");
            return;
        }

        Write(args, $"{rows.Count} within {Tuning.ScanRadius:0} m" + (all ? " (all)" : " (tameable)"));
        Write(args, RowFormatter.Header);

        foreach (NearbyEntity entity in rows)
        {
            Write(args, RowFormatter.Row(entity, scanner.Viewer, scanner.Forward));
        }
    }

    private static void Write(Terminal.ConsoleEventArgs args, string line)
    {
        args.Context?.AddString(line);
        Plugin.Log.LogInfo(line);
    }
}
