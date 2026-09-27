using System.Collections.Generic;
using NearbyPanel.Core;
using UnityEngine;

namespace NearbyPanel;

/// <summary>
/// Draws the list with legacy IMGUI. Read-only and inert: it never takes a click,
/// never grabs the cursor and is not draggable — see
/// docs/adr/0005-read-only-hud-no-mouse.md.
///
/// Cells are positioned at fixed pixel offsets rather than padded with spaces,
/// because Unity's built-in GUI font is proportional.
/// </summary>
internal sealed class PanelView
{
    private const float Padding = PanelLayout.Padding;

    /// <summary>
    /// Left edge of each column as a fraction of the inner width, plus a final 1.0
    /// so the last column has a right edge. NAME and STATUS carry the long text and
    /// get a third of the width each; the four numeric columns share the middle.
    /// </summary>
    private static readonly float[] ColumnEdges = { 0.00f, 0.34f, 0.46f, 0.55f, 0.63f, 0.68f, 1.00f };

    // Reused every draw: OnGUI runs at least twice a frame, and allocating a fresh
    // array per row per call is pure garbage.
    private readonly string[] _values = new string[6];

    private GUIStyle? _rowStyle;
    private GUIStyle? _rowStyleRight;
    private GUIStyle? _headerStyle;
    private GUIStyle? _boxStyle;
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
        GUI.Box(panel, GUIContent.none, _boxStyle);

        float y = panel.y + Padding;
        float innerWidth = placement.Width - (Padding * 2f);
        float x = panel.x + Padding;

        GUI.Label(new Rect(x, y, innerWidth, rowHeight), title, _headerStyle);
        y += rowHeight;

        DrawCells(x, y, innerWidth, rowHeight, RowFormatter.ColumnNames, header: true);
        y += rowHeight;

        for (int i = 0; i < shown; i++)
        {
            RowCells cells = rows[i];
            _values[0] = cells.Name;
            _values[1] = cells.Distance;
            _values[2] = cells.Direction;
            _values[3] = cells.Altitude;
            _values[4] = cells.Level;
            _values[5] = cells.Status;

            DrawCells(x, y, innerWidth, rowHeight, _values, header: false);
            y += rowHeight;
        }

        if (truncated)
        {
            GUI.Label(
                new Rect(x, y, innerWidth, rowHeight),
                "+" + (rows.Count - shown) + " more",
                _rowStyle);
        }
    }

    private void DrawCells(float x, float y, float width, float rowHeight, string[] values, bool header)
    {
        bool[] rightAligned = RowFormatter.ColumnRightAligned;

        for (int column = 0; column < values.Length && column + 1 < ColumnEdges.Length; column++)
        {
            float left = x + (ColumnEdges[column] * width);
            float right = x + (ColumnEdges[column + 1] * width);

            GUIStyle style = header
                ? _headerStyle!
                : column < rightAligned.Length && rightAligned[column] ? _rowStyleRight! : _rowStyle!;

            GUI.Label(new Rect(left, y, right - left, rowHeight), values[column], style);
        }
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
        };

        // Numbers read better flush right, and it matches the console dump.
        _rowStyleRight = new GUIStyle(_rowStyle)
        {
            alignment = TextAnchor.MiddleRight,
        };

        _headerStyle = new GUIStyle(_rowStyle)
        {
            fontStyle = FontStyle.Bold,
        };

        _boxStyle = new GUIStyle(GUI.skin.box);
    }
}
