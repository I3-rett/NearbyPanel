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
/// <param name="Taming">
/// Whether this row is an animal with taming under way. Carried as a flag rather
/// than left for the panel to infer from the text, so highlighting it does not
/// depend on searching the status string for a percent sign.
/// </param>
public sealed record RowCells(
    string Name,
    string Distance,
    string Direction,
    string Altitude,
    string Level,
    string Status,
    bool Taming = false);
