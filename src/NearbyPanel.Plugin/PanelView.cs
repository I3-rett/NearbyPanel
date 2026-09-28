using System.Collections.Generic;
using NearbyPanel.Core;
using UnityEngine;

namespace NearbyPanel;

/// <summary>
/// Draws the list with legacy IMGUI. Read-only and inert: it never takes a click,
/// never grabs the cursor and is not draggable. Dragging and wheel scrolling both
/// mean taking the cursor from the game and handing it back correctly on every
/// exit path, which is a large amount of fragile code for a list of a dozen rows.
///
/// Cells are positioned at fixed pixel offsets rather than padded with spaces,
/// because Unity's built-in GUI font is proportional.
///
/// Legibility over a moving game world needs more than default labels: a solid
/// backing, a bright text colour, alternating row tints so the eye can follow one
/// row across seven columns, and a red band on any hostile creature that has
/// noticed something — the row you want to catch without reading the table.
/// </summary>
internal sealed class PanelView
{
    private const float Padding = PanelLayout.Padding;

    /// <summary>
    /// Left edge of each column as a fraction of the inner width, plus a final 1.0
    /// so the last column has a right edge. NAME and STATUS carry the long text; the
    /// four numeric columns are kept narrow and sit together in the middle. ★ holds
    /// one digit and gives ALERT the room its bold heading needs.
    /// </summary>
    private static readonly float[] ColumnEdges =
        { 0.00f, 0.30f, 0.42f, 0.53f, 0.61f, 0.66f, 0.75f, 1.00f };

    private static readonly Color Background = new(0.07f, 0.06f, 0.05f, 0.88f);
    private static readonly Color RowTint = new(1f, 1f, 1f, 0.045f);
    private static readonly Color Rule = new(0.85f, 0.76f, 0.56f, 0.35f);
    private static readonly Color TextColour = new(0.93f, 0.90f, 0.83f, 1f);
    private static readonly Color HeaderColour = new(0.85f, 0.73f, 0.47f, 1f);
    private static readonly Color TitleColour = new(1f, 0.95f, 0.85f, 1f);
    private static readonly Color ProgressColour = new(1f, 0.72f, 0.36f, 1f);
    private static readonly Color ThreatTint = new(0.75f, 0.13f, 0.11f, 0.26f);
    private static readonly Color ThreatColour = new(1f, 0.55f, 0.48f, 1f);

    // Reused every draw: OnGUI runs at least twice a frame, and allocating a fresh
    // array per row per call is pure garbage.
    private readonly string[] _values = new string[7];

    // Cached because RowFormatter builds these arrays on every call and DrawCells
    // runs once per row, twice a frame: that was ~1500 throwaway arrays a second.
    private static readonly string[] Headings = RowFormatter.ColumnNames;
    private static readonly bool[] RightAligned = RowFormatter.ColumnRightAligned;

    private Texture2D? _pixel;
    private GUIStyle? _rowStyle;
    private GUIStyle? _rowStyleRight;
    private GUIStyle? _progressStyle;
    private GUIStyle? _threatStyle;
    private GUIStyle? _headerStyle;
    private GUIStyle? _titleStyle;
    private int _styleFontSize = -1;

    public void Draw(
        IReadOnlyList<RowCells> rows,
        PanelRect placement,
        int maxVisibleRows,
        int fontSize,
        string title)
    {
        EnsureStyles(fontSize);

        // Sizing and placement live in Core so they can be tested; this method only
        // draws what they decided.
        float rowHeight = PanelLayout.RowHeight(fontSize);
        int shown = PanelLayout.VisibleRows(rows.Count, maxVisibleRows);
        bool truncated = rows.Count > shown;

        Rect panel = new(placement.X, placement.Y, placement.Width, placement.Height);
        Fill(panel, Background);

        float y = panel.y + Padding;
        float innerWidth = placement.Width - (Padding * 2f);
        float x = panel.x + Padding;

        GUI.Label(new Rect(x, y, innerWidth, rowHeight), title, _titleStyle);
        y += rowHeight;

        DrawCells(x, y, innerWidth, rowHeight, Headings, _headerStyle!, null);
        y += rowHeight;

        // A hairline under the headings, so the table reads as a table.
        Fill(new Rect(x, y - 1f, innerWidth, 1f), Rule);

        for (int i = 0; i < shown; i++)
        {
            RowCells cells = rows[i];

            // A hostile creature that has noticed something gets a red band: this is
            // the row you want to catch without reading the table.
            if (cells.Threat)
            {
                Fill(new Rect(panel.x + 2f, y, placement.Width - 4f, rowHeight), ThreatTint);
            }
            else if (i % 2 == 1)
            {
                Fill(new Rect(panel.x + 2f, y, placement.Width - 4f, rowHeight), RowTint);
            }

            _values[0] = cells.Name;
            _values[1] = cells.Distance;
            _values[2] = cells.Direction;
            _values[3] = cells.Altitude;
            _values[4] = cells.Level;
            _values[5] = cells.Awareness;
            _values[6] = cells.Status;

            // A percentage is the thing worth spotting at a glance, so the
            // status cell is tinted for an animal being tamed.
            GUIStyle? statusStyle = cells.Progress ? _progressStyle : cells.Threat ? _threatStyle : null;

            DrawCells(x, y, innerWidth, rowHeight, _values, cells.Threat ? _threatStyle! : _rowStyle!, statusStyle);
            y += rowHeight;
        }

        if (truncated)
        {
            GUI.Label(
                new Rect(x, y, innerWidth, rowHeight),
                "+" + (rows.Count - shown) + " more",
                _headerStyle);
        }
    }

    /// <param name="lastColumnStyle">Overrides the style of the STATUS cell when set.</param>
    private void DrawCells(
        float x,
        float y,
        float width,
        float rowHeight,
        string[] values,
        GUIStyle normal,
        GUIStyle? lastColumnStyle)
    {
        bool[] rightAligned = RightAligned;
        bool isHeader = ReferenceEquals(normal, _headerStyle);

        for (int column = 0; column < values.Length && column + 1 < ColumnEdges.Length; column++)
        {
            float left = x + (ColumnEdges[column] * width);
            float right = x + (ColumnEdges[column + 1] * width);

            GUIStyle style = normal;

            if (!isHeader)
            {
                if (column == values.Length - 1 && lastColumnStyle != null)
                {
                    style = lastColumnStyle;
                }
                else if (column < rightAligned.Length && rightAligned[column])
                {
                    style = _rowStyleRight!;
                }
            }

            // Numeric columns are right-aligned in the header too, so a heading sits
            // over its own values rather than beside them.
            if (isHeader && column < rightAligned.Length && rightAligned[column])
            {
                style = _headerStyleRight!;
            }

            GUI.Label(new Rect(left, y, right - left, rowHeight), values[column], style);
        }
    }

    private GUIStyle? _headerStyleRight;

    private void Fill(Rect rect, Color colour)
    {
        if (_pixel == null)
        {
            // One white pixel, tinted per call. Cheaper and more predictable than
            // the game's skin, which is built for wood panels rather than tables.
            _pixel = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            _pixel.SetPixel(0, 0, Color.white);
            _pixel.Apply();
        }

        Color previous = GUI.color;
        GUI.color = colour;
        GUI.DrawTexture(rect, _pixel);
        GUI.color = previous;
    }

    private void EnsureStyles(int fontSize)
    {
        if (_rowStyle != null && _styleFontSize == fontSize)
        {
            return;
        }

        _styleFontSize = fontSize;

        _rowStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            alignment = TextAnchor.MiddleLeft,
            clipping = TextClipping.Clip,
            wordWrap = false,
            richText = false,
        };
        _rowStyle.normal.textColor = TextColour;

        // A little breathing room so text does not touch a column edge.
        _rowStyle.padding = new RectOffset(3, 3, 0, 0);

        _rowStyleRight = new GUIStyle(_rowStyle) { alignment = TextAnchor.MiddleRight };

        _progressStyle = new GUIStyle(_rowStyle);
        _progressStyle.normal.textColor = ProgressColour;

        _threatStyle = new GUIStyle(_rowStyle);
        _threatStyle.normal.textColor = ThreatColour;

        _headerStyle = new GUIStyle(_rowStyle) { fontStyle = FontStyle.Bold };
        _headerStyle.normal.textColor = HeaderColour;

        _headerStyleRight = new GUIStyle(_headerStyle) { alignment = TextAnchor.MiddleRight };

        _titleStyle = new GUIStyle(_rowStyle) { fontStyle = FontStyle.Bold };
        _titleStyle.normal.textColor = TitleColour;
    }
}
