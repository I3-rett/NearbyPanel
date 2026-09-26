using System.Globalization;

namespace NearbyPanel.Core;

/// <summary>
/// Renders one entity as the fixed-width columns the panel and the
/// <c>nearby_dump</c> console command both use. Shared on purpose: what you read
/// in the log is exactly what the panel shows.
/// </summary>
public static class RowFormatter
{
    public const string Header = "NAME                 DIST   DIR  ALT  LVL  STATUS";

    public static string Row(NearbyEntity entity, Vec3 viewer, Vec3 forward)
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

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0,-20} {1,5:0.0} {2,-4} {3,4:+0;-0;0} {4,3}  {5}",
            Truncate(entity.Name, 20),
            distance,
            direction,
            altitude,
            entity.Level,
            status);
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
