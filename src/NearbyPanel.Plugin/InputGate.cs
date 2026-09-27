using UnityEngine;

namespace NearbyPanel;

/// <summary>
/// Decides whether a hotkey press is meant for us or for something the player is
/// typing into. Without Jotunn there is no framework doing this, so it is spelled
/// out here against the game's own public checks.
///
/// Every member used is public and static except <c>Chat.HasFocus</c>, which is
/// reached through the public static <c>Chat.instance</c>.
/// </summary>
internal static class InputGate
{
    /// <summary>
    /// Whether an IMGUI control held keyboard focus the last time any OnGUI ran.
    ///
    /// <c>GUIUtility.keyboardControl</c> is only meaningful inside OnGUI; read from
    /// Update it returns whatever was left over, which is not a reliable answer and
    /// can block the hotkey forever. So it is sampled where it is valid and read
    /// where it is needed.
    /// </summary>
    private static bool _imguiHasFocus;

    /// <summary>Call once at the top of OnGUI, before any early return.</summary>
    public static void SampleGuiFocus() => _imguiHasFocus = GUIUtility.keyboardControl != 0;

    /// <summary>
    /// True when a keypress should be treated as a command to this mod rather than
    /// text the player is entering or a menu they are driving.
    /// </summary>
    public static bool AcceptsHotkey() => Blocker() == null;

    /// <summary>
    /// Which check, if any, is currently swallowing the hotkey. Returns null when
    /// the press should be accepted. Exposed so a rejected press can say why
    /// instead of vanishing — a hotkey that silently does nothing is otherwise
    /// indistinguishable from a mod that failed to load.
    /// </summary>
    public static string? Blocker()
    {
        if (!Player.m_localPlayerExists || Player.m_localPlayer == null)
        {
            return "no local player";
        }

        if (Chat.instance != null && Chat.instance.HasFocus())
        {
            return "chat has focus";
        }

        if (Console.IsVisible())
        {
            return "console is open";
        }

        if (TextInput.IsVisible())
        {
            return "a text prompt is open";
        }

        if (Menu.IsVisible())
        {
            return "the menu is open";
        }

        if (InventoryGui.IsVisible())
        {
            return "the inventory is open";
        }

        if (_imguiHasFocus)
        {
            return "an IMGUI text field has focus";
        }

        if (Player.m_localPlayer.InPlaceMode())
        {
            return "placing a building piece";
        }

        return null;
    }

    /// <summary>
    /// True when the panel should be drawn at all. Separate from
    /// <see cref="AcceptsHotkey"/> because the panel stays visible with the
    /// inventory open, it just must not react to keys.
    /// </summary>
    public static bool ShouldDraw() =>
        Player.m_localPlayerExists
        && Player.m_localPlayer != null
        && !Menu.IsVisible();
}
