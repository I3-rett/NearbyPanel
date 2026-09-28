namespace NearbyPanel.Core;

/// <summary>
/// One row already reduced to display strings, one per column.
///
/// This exists because the two consumers need the same values in different
/// layouts. The console dump writes them into fixed-width columns, which works
/// because a log file is monospaced. The panel cannot do that — Unity's built-in
/// GUI font is proportional, so padded text does not line up — so it draws each
/// cell at its own pixel offset instead. Sharing the cells rather than the
/// formatted line keeps the two honest with each other.
/// </summary>
/// <param name="Progress">
/// Whether the status cell carries a percentage — taming under way, or a young
/// animal growing. Carried as a flag rather than left for the panel to infer from
/// the text, so highlighting it does not depend on searching for a percent sign.
/// </param>
/// <param name="Awareness">The AI column: calm, tracking, alerted, or alerted at you.</param>
/// <param name="Threat">
/// Whether this row deserves a warning tint: hostile to you *and* alerted. Carried
/// as a flag so the panel does not have to re-derive it from text.
/// </param>
public sealed record RowCells(
    string Name,
    string Distance,
    string Direction,
    string Altitude,
    string Level,
    string Awareness,
    string Status,
    bool Progress = false,
    bool Threat = false);
