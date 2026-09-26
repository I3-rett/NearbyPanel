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
    private const float Padding = 8f;
    private const float RowHeight = 18f;

    /// <summary>Left offset of each column, as a fraction of the panel width.</summary>
    private static readonly float[] ColumnOffsets = { 0.00f, 0.42f, 0.55f, 0.64f, 0.73f, 0.80f };

    private GUIStyle? _rowStyle;
    private GUIStyle? _headerStyle;
    private GUIStyle? _boxStyle;

    public void Draw(
        IReadOnlyList<RowCells> rows,
        Vector2 anchor,
        float width,
        int maxVisibleRows,
        int fontSize,
        string title)
    {
        EnsureStyles(fontSize);

        int shown = Mathf.Min(rows.Count, maxVisibleRows);
        bool truncated = rows.Count > shown;

        // title + header + rows (+ a line saying how many were hidden)
        float lines = 2f + shown + (truncated ? 1f : 0f);
        float height = (Padding * 2f) + (lines * RowHeight);

        Rect panel = new(anchor.x, anchor.y, width, height);
        GUI.Box(panel, GUIContent.none, _boxStyle);

        float y = panel.y + Padding;
        float innerWidth = width - (Padding * 2f);

        GUI.Label(new Rect(panel.x + Padding, y, innerWidth, RowHeight), title, _headerStyle);
        y += RowHeight;

        DrawCells(panel.x + Padding, y, innerWidth, RowFormatter.ColumnNames, _headerStyle!);
        y += RowHeight;

        for (int i = 0; i < shown; i++)
        {
            RowCells cells = rows[i];
            string[] values =
            {
                cells.Name,
                cells.Distance,
                cells.Direction,
                cells.Altitude,
                cells.Level,
                cells.Status,
            };

            DrawCells(panel.x + Padding, y, innerWidth, values, _rowStyle!);
            y += RowHeight;
        }

        if (truncated)
        {
            GUI.Label(
                new Rect(panel.x + Padding, y, innerWidth, RowHeight),
                "+" + (rows.Count - shown) + " more",
                _rowStyle);
        }
    }

    private static void DrawCells(float x, float y, float width, string[] values, GUIStyle style)
    {
        for (int column = 0; column < values.Length && column < ColumnOffsets.Length; column++)
        {
            float left = x + (ColumnOffsets[column] * width);
            float right = column + 1 < ColumnOffsets.Length
                ? x + (ColumnOffsets[column + 1] * width)
                : x + width;

            GUI.Label(new Rect(left, y, right - left, RowHeight), values[column], style);
        }
    }

    private void EnsureStyles(int fontSize)
    {
        if (_rowStyle != null && _rowStyle.fontSize == fontSize)
        {
            return;
        }

        _rowStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = fontSize,
            alignment = TextAnchor.MiddleLeft,
            clipping = TextClipping.Clip,
            wordWrap = false,
        };

        _headerStyle = new GUIStyle(_rowStyle)
        {
            fontStyle = FontStyle.Bold,
        };

        _boxStyle = new GUIStyle(GUI.skin.box);
    }
}
