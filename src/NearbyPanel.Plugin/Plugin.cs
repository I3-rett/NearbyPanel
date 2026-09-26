using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace NearbyPanel;

/// <summary>
/// Entry point. Client-side only: this plugin reads local component and ZDO state
/// and draws a panel. It registers no RPC, writes no ZDO and takes part in no
/// version handshake, so it never affects joining a server that does not have it.
/// </summary>
[BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
public sealed class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log = null!;

    internal static ConfigEntry<bool> Enabled = null!;

    private void Awake()
    {
        Log = Logger;

        Enabled = Config.Bind(
            "General",
            "Enabled",
            true,
            "Master switch. Turning this off hides the panel and stops the scan.");

        Log.LogInfo(PluginInfo.Name + " " + PluginInfo.Version + " loaded (client-side only).");
    }
}
