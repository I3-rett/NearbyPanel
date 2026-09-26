namespace NearbyPanel;

/// <summary>
/// Single source of truth for the plugin identity. package/manifest.json must
/// carry the same version string; build/Package.ps1 checks that they agree.
/// </summary>
internal static class PluginInfo
{
    public const string Guid = "siam.NearbyPanel";
    public const string Name = "NearbyPanel";
    public const string Version = "0.2.0";
}
