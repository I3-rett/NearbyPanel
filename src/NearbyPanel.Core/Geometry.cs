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

    /// <summary>Height of <paramref name="to"/> relative to <paramref name="from"/>.</summary>
    public static float HeightDelta(Vec3 from, Vec3 to) => to.Y - from.Y;

    /// <summary>
    /// Bearing to <paramref name="to"/> relative to the direction the viewer faces,
    /// in degrees within (-180, 180]. Negative is to the left, positive to the right.
    /// Returns 0 when the two positions coincide horizontally.
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
        double degrees = (target - facing) * 180.0 / Math.PI;

        // Normalise into (-180, 180].
        while (degrees <= -180.0)
        {
            degrees += 360.0;
        }

        while (degrees > 180.0)
        {
            degrees -= 360.0;
        }

        return (float)degrees;
    }

    /// <summary>
    /// The bearing as one of the eight compass points, for a readable column.
    /// Uses 45 degree sectors centred on each point.
    /// </summary>
    public static string CompassPoint(float relativeBearing)
    {
        float b = relativeBearing;
        while (b < 0f)
        {
            b += 360f;
        }

        int sector = (int)Math.Round(b / 45f) % 8;
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
}
