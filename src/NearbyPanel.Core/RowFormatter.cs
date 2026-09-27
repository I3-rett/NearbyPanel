using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace NearbyPanel.Core;

/// <summary>
/// Turns an entity into display text. <see cref="Cells"/> is the shared source of
/// truth; <see cref="Row"/> lays those cells out in fixed-width columns for the
/// <c>nearby_dump</c> console command, and the panel positions the same cells
/// itself. See <see cref="RowCells"/> for why the two differ.
/// </summary>
public static class RowFormatter
{
    /// <summary>
    /// The columns, in order, with the width the fixed-width layout gives each.
    /// The header and every row are both generated from this, so they cannot drift
    /// apart — they used to be two hand-written literals and the header sat a
    /// column to the right of its own data.
    /// </summary>
    private static readonly Column[] Layout =
    {
        new("NAME", 18, RightAligned: false),
        new("DIST", 6, RightAligned: true),
        // 5 wide because "-135°" is the longest the degrees format produces.
        new("DIR", 5, RightAligned: true),
        new("ALT", 4, RightAligned: true),
        new("★", 2, RightAligned: true),
        new("AI", 4, RightAligned: false),
        new("STATUS", 0, RightAligned: false),
    };

    /// <summary>Column headings, in the same order as <see cref="RowCells"/>.</summary>
    public static string[] ColumnNames => Layout.Select(c => c.Name).ToArray();

    /// <summary>Whether each column's value reads better right-aligned.</summary>
    public static bool[] ColumnRightAligned => Layout.Select(c => c.RightAligned).ToArray();

    /// <summary>The heading line for the fixed-width layout.</summary>
    public static string Header => Compose(Layout.Select(c => c.Name).ToArray());

    public static RowCells Cells(
        NearbyEntity entity,
        Vec3 viewer,
        Vec3 forward,
        DirectionFormat format = DirectionFormat.Degrees)
    {
        float distance = Geometry.GroundDistance(viewer, entity.Position);
        float altitude = Geometry.HeightDelta(viewer, entity.Position);
        string direction = Direction(entity, viewer, forward, format);

        string status = entity.Status ?? string.Empty;
        if (entity.TamingProgress is { } progress)
        {
            string percent = Percent(progress);
            status = status.Length == 0 ? percent : percent + " " + status;
        }

        if (status.Length == 0 && entity.Hostile)
        {
            // Non-tameables have nothing else to put here, and "would attack me"
            // is not always obvious from the name: an aggravated dvergr looks the
            // same as a neutral one.
            status = "hostile";
        }

        return new RowCells(
            Name: entity.Name ?? string.Empty,
            Distance: Format(distance, "0.0"),
            Direction: direction,
            Altitude: Format(altitude, "+0;-0;0"),
            Level: Stars(entity.Level),
            Awareness: AwarenessLabel(entity),
            Status: status,
            Taming: entity.TamingProgress.HasValue,
            Threat: entity.Hostile && entity.Awareness == Core.Awareness.Alerted);
    }

    /// <summary>How the DIR cell reads, for the chosen format.</summary>
    private static string Direction(NearbyEntity entity, Vec3 viewer, Vec3 forward, DirectionFormat format)
    {
        if (format == DirectionFormat.Compass)
        {
            return Geometry.CompassPoint(Geometry.CompassBearing(viewer, entity.Position));
        }

        float bearing = Geometry.RelativeBearing(viewer, forward, entity.Position);

        return format == DirectionFormat.Relative
            ? Geometry.RelativeHeading(bearing)
            : Geometry.DegreesLabel(bearing);
    }

    /// <summary>The fixed-width form, for the console and the log.</summary>
    public static string Row(
        NearbyEntity entity,
        Vec3 viewer,
        Vec3 forward,
        DirectionFormat format = DirectionFormat.Degrees)
    {
        RowCells cells = Cells(entity, viewer, forward, format);

        return Compose(new[]
        {
            cells.Name,
            cells.Distance,
            cells.Direction,
            cells.Altitude,
            cells.Level,
            cells.Awareness,
            cells.Status,
        });
    }

    /// <summary>
    /// The AI column. "!you" only when the game can actually say so: a creature's
    /// target is held by the peer simulating it, so for anything owned by the
    /// server or another player the answer is unknown, and unknown is shown as a
    /// plain alert rather than as safety.
    /// </summary>
    public static string AwarenessLabel(NearbyEntity entity) => entity.Awareness switch
    {
        Core.Awareness.Alerted => entity.TargetsYou == true ? "!you" : "!",
        Core.Awareness.Tracking => "?",
        _ => "-",
    };

    /// <summary>
    /// Star rating, which is the creature's level minus one — vanilla stores an
    /// ordinary creature at level 1 and every star display in the game subtracts one.
    /// </summary>
    public static string Stars(int level)
    {
        int stars = level - 1;
        return stars <= 0 ? "-" : stars.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Taming progress as whole percent. Truncated, not rounded, to match the
    /// game's own <c>Tameable.GetTameness()</c>, which casts to int.
    /// </summary>
    public static string Percent(float progress)
    {
        if (float.IsNaN(progress))
        {
            return "?";
        }

        int whole = (int)(Clamp01(progress) * 100f);
        return whole.ToString(CultureInfo.InvariantCulture) + "%";
    }

    /// <summary>
    /// Shortens <paramref name="value"/> to <paramref name="max"/> characters,
    /// marking the cut with an ellipsis. A <paramref name="max"/> below 1 yields an
    /// empty string rather than throwing.
    /// </summary>
    public static string Truncate(string value, int max)
    {
        if (value == null || max <= 0)
        {
            return string.Empty;
        }

        if (value.Length <= max)
        {
            return value;
        }

        return max == 1 ? "…" : value.Substring(0, max - 1) + "…";
    }

    /// <summary>Lays out one line of values against <see cref="Layout"/>.</summary>
    private static string Compose(string[] values)
    {
        StringBuilder builder = new();

        for (int i = 0; i < Layout.Length; i++)
        {
            Column column = Layout[i];
            string value = i < values.Length ? values[i] ?? string.Empty : string.Empty;

            if (i > 0)
            {
                builder.Append(' ');
            }

            // Width 0 means "the rest of the line": no padding, no truncation.
            if (column.Width <= 0)
            {
                builder.Append(value);
                continue;
            }

            value = Truncate(value, column.Width);
            builder.Append(column.RightAligned
                ? value.PadLeft(column.Width)
                : value.PadRight(column.Width));
        }

        return builder.ToString().TrimEnd();
    }

    private static string Format(float value, string format)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            return "?";
        }

        return value.ToString(format, CultureInfo.InvariantCulture);
    }

    private static float Clamp01(float value) =>
        value < 0f ? 0f : value > 1f ? 1f : value;

    /// <param name="Width">Characters in the fixed-width layout; 0 means the rest of the line.</param>
    private readonly record struct Column(string Name, int Width, bool RightAligned);

    /// <summary>The character offset at which each column starts. For tests.</summary>
    public static int[] ColumnOffsets()
    {
        int[] offsets = new int[Layout.Length];
        int at = 0;

        for (int i = 0; i < Layout.Length; i++)
        {
            offsets[i] = at;
            at += Math.Max(Layout[i].Width, 0) + 1;
        }

        return offsets;
    }
}
