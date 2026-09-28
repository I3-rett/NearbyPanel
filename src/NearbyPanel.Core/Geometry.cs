using System;

namespace NearbyPanel.Core;

/// <summary>Distance and bearing maths. Pure, so fully unit tested.</summary>
public static class Geometry
{
    /// <summary>Straight-line distance, ignoring height.</summary>
    public static float GroundDistance(Vec3 from, Vec3 to)
    {
        float dx = to.X - from.X;
        float dz = to.Z - from.Z;
        return (float)Math.Sqrt((dx * dx) + (dz * dz));
    }

    /// <summary>
    /// Straight-line distance including height. This is the metric the game's own
    /// <c>Character.GetCharactersInRange</c> uses, so membership of the list is
    /// decided with it; <see cref="GroundDistance"/> is only for display. Using
    /// ground distance to filter would promise a 50 m disc while the game had
    /// already withheld everything outside a 50 m sphere.
    /// </summary>
    public static float Distance(Vec3 from, Vec3 to)
    {
        float dx = to.X - from.X;
        float dy = to.Y - from.Y;
        float dz = to.Z - from.Z;
        return (float)Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
    }

    /// <summary>Height of <paramref name="to"/> relative to <paramref name="from"/>.</summary>
    public static float HeightDelta(Vec3 from, Vec3 to) => to.Y - from.Y;

    /// <summary>
    /// Bearing to <paramref name="to"/> relative to the direction the viewer faces,
    /// in degrees within (-180, 180]. Negative is to the left, positive to the right.
    /// Returns 0 when the two positions coincide horizontally, and also when
    /// <paramref name="forward"/> is degenerate — a zero forward reads as "facing
    /// north" rather than throwing.
    /// </summary>
    public static float RelativeBearing(Vec3 from, Vec3 forward, Vec3 to)
    {
        float dx = to.X - from.X;
        float dz = to.Z - from.Z;
        if (dx == 0f && dz == 0f)
        {
            return 0f;
        }

        double target = Math.Atan2(dx, dz);
        double facing = Math.Atan2(forward.X, forward.Z);
        return (float)Normalise((target - facing) * 180.0 / Math.PI);
    }

    /// <summary>
    /// The bearing as one of eight labels **relative to where the viewer is facing**:
    /// F ahead, R right, B behind, L left, and the diagonals. Deliberately not
    /// compass letters — N/E/S/W would sit next to a real-world altitude column and
    /// be read as world directions, which these are not.
    /// </summary>
    public static string RelativeHeading(float relativeBearing) => RelativeSector(relativeBearing) switch
    {
        0 => "F",
        1 => "FR",
        2 => "R",
        3 => "BR",
        4 => "B",
        5 => "BL",
        6 => "L",
        _ => "FL",
    };

    /// <summary>
    /// The bearing as one of eight arrows, as the target sits on screen: ↑ ahead,
    /// → right, ↓ behind. The same sectors as <see cref="RelativeHeading"/>, read
    /// without having to decode letters.
    /// </summary>
    public static string RelativeArrow(float relativeBearing)
    {
        if (float.IsNaN(relativeBearing) || float.IsInfinity(relativeBearing))
        {
            return "?";
        }

        return RelativeSector(relativeBearing) switch
        {
            0 => "↑",
            1 => "↗",
            2 => "→",
            3 => "↘",
            4 => "↓",
            5 => "↙",
            6 => "←",
            _ => "↖",
        };
    }

    /// <summary>Which eighth of a turn a relative bearing falls in, 0 ahead, clockwise.</summary>
    private static int RelativeSector(float relativeBearing)
    {
        double bearing = Normalise(relativeBearing);
        if (bearing < 0.0)
        {
            bearing += 360.0;
        }

        // Round half away from zero, so every label owns a symmetric 45 degree
        // sector. Math.Round would use banker's rounding, which makes the sector
        // at 22.5 degrees behave differently from the one at 67.5.
        return (int)Math.Floor((bearing / 45.0) + 0.5) % 8;
    }

    /// <summary>
    /// The world compass bearing from <paramref name="from"/> to
    /// <paramref name="to"/>, in degrees clockwise from north, within [0, 360).
    ///
    /// North is +Z in Valheim, which is the convention the map uses.
    /// </summary>
    public static float CompassBearing(Vec3 from, Vec3 to)
    {
        float dx = to.X - from.X;
        float dz = to.Z - from.Z;
        if (dx == 0f && dz == 0f)
        {
            return 0f;
        }

        double degrees = Math.Atan2(dx, dz) * 180.0 / Math.PI;
        if (degrees < 0.0)
        {
            degrees += 360.0;
        }

        return (float)degrees;
    }

    /// <summary>
    /// A world compass bearing as one of eight points. Unlike
    /// <see cref="RelativeHeading"/> this does not move when you turn.
    /// </summary>
    public static string CompassPoint(float compassBearing)
    {
        double bearing = compassBearing % 360.0;
        if (bearing < 0.0)
        {
            bearing += 360.0;
        }

        int sector = (int)Math.Floor((bearing / 45.0) + 0.5) % 8;
        return sector switch
        {
            0 => "N",
            1 => "NE",
            2 => "E",
            3 => "SE",
            4 => "S",
            5 => "SW",
            6 => "W",
            _ => "NW",
        };
    }

    /// <summary>
    /// A relative bearing written in degrees: <c>0°</c> ahead, <c>90°</c> to the
    /// right, <c>-90°</c> to the left, <c>180°</c> behind. The sign carries the
    /// side, so there is no need for a separate left/right marker.
    /// </summary>
    public static string DegreesLabel(float relativeBearing)
    {
        if (float.IsNaN(relativeBearing) || float.IsInfinity(relativeBearing))
        {
            return "?";
        }

        double degrees = Normalise(relativeBearing);

        // Round half away from zero so -0.5 does not print as "0" on one side and
        // "-1" on the other.
        int whole = (int)(degrees < 0 ? Math.Ceiling(degrees - 0.5) : Math.Floor(degrees + 0.5));

        // Directly behind is 180, never -180, matching RelativeBearing.
        if (whole == -180)
        {
            whole = 180;
        }

        return whole.ToString(System.Globalization.CultureInfo.InvariantCulture) + "°";
    }

    /// <summary>Folds any angle in degrees into (-180, 180].</summary>
    private static double Normalise(double degrees)
    {
        // Modulo rather than a loop: a loop would spin for a very large magnitude.
        degrees %= 360.0;

        if (degrees <= -180.0)
        {
            degrees += 360.0;
        }
        else if (degrees > 180.0)
        {
            degrees -= 360.0;
        }

        return degrees;
    }
}
