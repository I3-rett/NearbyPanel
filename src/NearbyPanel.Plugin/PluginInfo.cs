namespace NearbyPanel;

/// <summary>
/// Single source of truth for the plugin identity. package/manifest.json must
/// carry the same version string; build/Package.ps1 checks that they agree.
/// </summary>
internal static class PluginInfo
{
    public const string Guid = "I3_rett.NearbyPanel";
    public const string Name = "NearbyPanel";
    public const string Version = "1.0.0";
}
