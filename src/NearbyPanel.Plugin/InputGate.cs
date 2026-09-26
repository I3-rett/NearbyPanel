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
    /// True when a keypress should be treated as a command to this mod rather than
    /// text the player is entering or a menu they are driving.
    /// </summary>
    public static bool AcceptsHotkey()
    {
        if (!Player.m_localPlayerExists || Player.m_localPlayer == null)
        {
            return false;
        }

        if (Chat.instance != null && Chat.instance.HasFocus())
        {
            return false;
        }

        if (Console.IsVisible() || TextInput.IsVisible())
        {
            return false;
        }

        if (Menu.IsVisible() || InventoryGui.IsVisible())
        {
            return false;
        }

        // Placing a building piece swallows most input; leave it alone.
        return !Player.m_localPlayer.InPlaceMode();
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
