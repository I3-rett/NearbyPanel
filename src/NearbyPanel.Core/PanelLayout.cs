namespace NearbyPanel.Core;

/// <summary>Which corner of the screen the panel is pinned to.</summary>
public enum PanelAnchor
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}

/// <summary>Where the panel ends up, in screen pixels.</summary>
public readonly struct PanelRect
{
    public PanelRect(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public float X { get; }

    public float Y { get; }

    public float Width { get; }

    public float Height { get; }
}

/// <summary>
/// Sizing and placement arithmetic. Pure, so the one thing that decides whether
/// the panel is visible at all can be tested without a screen.
/// </summary>
public static class PanelLayout
{
    public const float Padding = 8f;

    /// <summary>Row height for a given font size, never smaller than legible.</summary>
    public static float RowHeight(int fontSize)
    {
        float height = fontSize * 1.35f;
        return height < 14f ? 14f : height;
    }

    /// <summary>How many rows are actually drawn, given the cap.</summary>
    public static int VisibleRows(int rowCount, int maxVisibleRows)
    {
        if (rowCount < 0)
        {
            return 0;
        }

        int max = maxVisibleRows < 0 ? 0 : maxVisibleRows;
        return rowCount < max ? rowCount : max;
    }

    /// <summary>
    /// Total height: a title line, a header line, the visible rows, and one more
    /// line for the "+N more" note when the list was cut short.
    /// </summary>
    public static float Height(int rowCount, int maxVisibleRows, int fontSize)
    {
        int shown = VisibleRows(rowCount, maxVisibleRows);
        bool truncated = rowCount > shown;
        float lines = 2f + shown + (truncated ? 1f : 0f);

        return (Padding * 2f) + (lines * RowHeight(fontSize));
    }

    /// <summary>
    /// Places the panel against <paramref name="anchor"/>, inset by the margins,
    /// and keeps it on screen. Margins are measured from the anchored corner, so
    /// the same numbers behave the same way whichever corner is chosen.
    ///
    /// Clamping matters: an oversized panel or a stray margin would otherwise sit
    /// off-screen with no way to bring it back except editing the config file.
    /// </summary>
    public static PanelRect Place(
        PanelAnchor anchor,
        float marginX,
        float marginY,
        float width,
        float height,
        float screenWidth,
        float screenHeight)
    {
        float w = Clamp(width, 0f, screenWidth);
        float h = Clamp(height, 0f, screenHeight);

        float mx = marginX < 0f ? 0f : marginX;
        float my = marginY < 0f ? 0f : marginY;

        bool right = anchor == PanelAnchor.TopRight || anchor == PanelAnchor.BottomRight;
        bool bottom = anchor == PanelAnchor.BottomLeft || anchor == PanelAnchor.BottomRight;

        float x = right ? screenWidth - w - mx : mx;
        float y = bottom ? screenHeight - h - my : my;

        return new PanelRect(
            Clamp(x, 0f, Max(0f, screenWidth - w)),
            Clamp(y, 0f, Max(0f, screenHeight - h)),
            w,
            h);
    }

    private static float Clamp(float value, float min, float max) =>
        value < min ? min : value > max ? max : value;

    private static float Max(float a, float b) => a > b ? a : b;
}
