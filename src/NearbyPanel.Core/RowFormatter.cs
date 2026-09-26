using System.Globalization;

namespace NearbyPanel.Core;

/// <summary>
/// Turns an entity into display text. <see cref="Cells"/> is the shared source of
/// truth; <see cref="Row"/> lays those cells out in fixed-width columns for the
/// <c>nearby_dump</c> console command, and the panel positions the same cells
/// itself. See <see cref="RowCells"/> for why the two differ.
/// </summary>
public static class RowFormatter
{
    public const string Header = "NAME                 DIST   DIR  ALT  LVL  STATUS";

    /// <summary>Column headings, in the same order as <see cref="RowCells"/>.</summary>
    public static readonly string[] ColumnNames = { "NAME", "DIST", "DIR", "ALT", "LVL", "STATUS" };

    public static RowCells Cells(NearbyEntity entity, Vec3 viewer, Vec3 forward)
    {
        float distance = Geometry.GroundDistance(viewer, entity.Position);
        float altitude = Geometry.HeightDelta(viewer, entity.Position);
        string direction = Geometry.CompassPoint(Geometry.RelativeBearing(viewer, forward, entity.Position));

        string status = entity.Status ?? string.Empty;
        if (entity.TamingProgress is { } progress)
        {
            string percent = Percent(progress);
            status = status.Length == 0 ? percent : percent + " " + status;
        }

        return new RowCells(
            Name: entity.Name,
            Distance: distance.ToString("0.0", CultureInfo.InvariantCulture),
            Direction: direction,
            Altitude: altitude.ToString("+0;-0;0", CultureInfo.InvariantCulture),
            Level: entity.Level.ToString(CultureInfo.InvariantCulture),
            Status: status);
    }

    /// <summary>The fixed-width form, for the console and the log.</summary>
    public static string Row(NearbyEntity entity, Vec3 viewer, Vec3 forward)
    {
        RowCells cells = Cells(entity, viewer, forward);

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0,-20} {1,5} {2,-4} {3,4} {4,3}  {5}",
            Truncate(cells.Name, 20),
            cells.Distance,
            cells.Direction,
            cells.Altitude,
            cells.Level,
            cells.Status);
    }

    /// <summary>
    /// Taming progress as whole percent. Truncated, not rounded, to match the
    /// game's own <c>Tameable.GetTameness()</c>, which casts to int.
    /// </summary>
    public static string Percent(float progress)
    {
        int whole = (int)(Clamp01(progress) * 100f);
        return whole.ToString(CultureInfo.InvariantCulture) + "%";
    }

    public static string Truncate(string value, int max) =>
        value.Length <= max ? value : value.Substring(0, max - 1) + "…";

    private static float Clamp01(float value) =>
        value < 0f ? 0f : value > 1f ? 1f : value;
}
