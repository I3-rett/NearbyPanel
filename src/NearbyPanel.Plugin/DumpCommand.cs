using System.Collections.Generic;
using NearbyPanel.Core;

namespace NearbyPanel;

/// <summary>
/// The <c>nearby_dump</c> console command. Writes the current list to both the
/// console and <c>LogOutput.log</c>, using the same formatter the panel uses, so
/// the log is a faithful record of what the panel showed.
///
/// It applies the same filter as the panel, deliberately. An earlier version took
/// an <c>all</c> argument that dropped the filter and listed every creature in
/// range — which is precisely the creature radar
/// docs/adr/0003-fixed-radius-and-filters.md exists to prevent, reachable by typing
/// one extra word. A console argument is a weaker gate than the slider that ADR
/// already rejects.
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
        IReadOnlyList<NearbyEntity> rows = scanner.Scan(NearbyList.IsTameable);

        if (rows.Count == 0)
        {
            Write(args, Player.m_localPlayer == null
                ? "no local player yet"
                : $"nothing within {Tuning.ScanRadius:0} m");
            return;
        }

        Write(args, $"{rows.Count} within {Tuning.ScanRadius:0} m");
        Write(args, RowFormatter.Header);

        foreach (NearbyEntity entity in rows)
        {
            Write(args, RowFormatter.Row(entity, scanner.Viewer, scanner.Forward));
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
