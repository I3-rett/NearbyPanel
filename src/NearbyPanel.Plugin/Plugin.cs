using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using NearbyPanel.Core;
using UnityEngine;

namespace NearbyPanel;

/// <summary>
/// Entry point. Client-side only: this plugin reads local component and network
/// state and draws a panel. It registers no RPC, writes nothing and takes part in
/// no version handshake, so it never affects joining a server that does not have it.
/// </summary>
[BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
public sealed class Plugin : BaseUnityPlugin
{
    /// <summary>How often the list is rebuilt while the panel is open.</summary>
    private const float RefreshSeconds = 0.25f;

    internal static ManualLogSource Log = null!;

    internal static ConfigEntry<bool> Enabled = null!;

    private static ConfigEntry<KeyboardShortcut> _toggleKey = null!;
    private static ConfigEntry<bool> _openOnStart = null!;
    private static ConfigEntry<int> _maxVisibleRows = null!;
    private static ConfigEntry<int> _fontSize = null!;
    private static ConfigEntry<float> _panelWidth = null!;
    private static ConfigEntry<float> _marginX = null!;
    private static ConfigEntry<float> _marginY = null!;

    private readonly EntityScanner _scanner = new();
    private readonly PanelView _view = new();
    private readonly List<RowCells> _rows = new();

    private bool _open;
    private float _sinceRefresh;
    private bool _hadPlayer;
    private string _title = string.Empty;

    private void Awake()
    {
        Log = Logger;

        Enabled = Config.Bind(
            "General",
            "Enabled",
            true,
            "Master switch. Turning this off hides the panel and stops the scan.");

        _toggleKey = Config.Bind(
            "General",
            "Toggle key",
            new KeyboardShortcut(KeyCode.N),
            "Shows and hides the panel. Ignored while typing in chat or the console. "
            + "Note that a shortcut with no modifier will not fire while a modifier is held.");

        _openOnStart = Config.Bind(
            "General",
            "Open on start",
            false,
            "Whether the panel is already showing when you load into a world.");

        FilterState.Bind(Config.Bind(
            "General",
            "Name filter",
            string.Empty,
            "Only list creatures whose name contains this text, case-insensitively. "
            + "Empty lists everything. Also settable in game with: nearby_filter <text>"));

        _maxVisibleRows = Config.Bind(
            "Panel",
            "Visible rows",
            12,
            new ConfigDescription(
                "Rows drawn before the list is cut short with a count of the rest.",
                new AcceptableValueRange<int>(1, Tuning.MaxRows)));

        _fontSize = Config.Bind(
            "Panel",
            "Font size",
            14,
            new ConfigDescription("Text size in the panel.", new AcceptableValueRange<int>(8, 32)));

        _panelWidth = Config.Bind(
            "Panel",
            "Width",
            460f,
            new ConfigDescription("Panel width in pixels.", new AcceptableValueRange<float>(240f, 1200f)));

        _marginX = Config.Bind(
            "Panel",
            "Margin X",
            12f,
            new ConfigDescription(
                "Distance from the left edge of the screen, in pixels.",
                new AcceptableValueRange<float>(0f, 4000f)));

        _marginY = Config.Bind(
            "Panel",
            "Margin Y",
            120f,
            new ConfigDescription(
                "Distance from the top edge of the screen, in pixels.",
                new AcceptableValueRange<float>(0f, 4000f)));

        _open = _openOnStart.Value;

        // Constructing a command registers it with the terminal, which keeps its
        // own static table, so this is safe to do before any world is loaded.
        DumpCommand.Register(_scanner);
        FilterCommand.Register();

        Log.LogInfo(PluginInfo.Name + " " + PluginInfo.Version + " loaded (client-side only).");
    }

    private void Update()
    {
        bool hasPlayer = Player.m_localPlayerExists && Player.m_localPlayer != null;
        if (_hadPlayer && !hasPlayer)
        {
            _rows.Clear();
        }

        _hadPlayer = hasPlayer;

        if (!Enabled.Value)
        {
            _open = false;
            return;
        }

        if (_toggleKey.Value.IsDown())
        {
            string? blocked = InputGate.Blocker();
            if (blocked == null)
            {
                _open = !_open;
                _sinceRefresh = RefreshSeconds; // refresh on the frame it opens
                Log.LogInfo("Panel " + (_open ? "opened" : "closed") + ".");
            }
            else
            {
                // A hotkey that silently does nothing is indistinguishable from a
                // mod that failed to load, so say which check swallowed it.
                Log.LogInfo("Toggle key ignored: " + blocked + ".");
            }
        }

        // Nothing is scanned while the panel is closed: no work, no allocation.
        if (!_open || !InputGate.ShouldDraw())
        {
            return;
        }

        _sinceRefresh += Time.unscaledDeltaTime;
        if (_sinceRefresh < RefreshSeconds)
        {
            return;
        }

        _sinceRefresh = 0f;
        Refresh();
    }

    private void Refresh()
    {
        try
        {
            IReadOnlyList<NearbyEntity> entities = _scanner.Scan(FilterState.Predicate());

            _rows.Clear();
            foreach (NearbyEntity entity in entities)
            {
                _rows.Add(RowFormatter.Cells(entity, _scanner.Viewer, _scanner.Forward));
            }

            // Built once per refresh rather than per OnGUI call, of which there are
            // at least two a frame.
            string scope = Tuning.RadiusLabel + " m" + FilterState.Describe();
            _title = _rows.Count == 0
                ? "Nearby — nothing within " + scope
                : "Nearby — " + _rows.Count + " within " + scope;

            if (_scanner.Faulted)
            {
                _title += " (partial)";
            }
        }
        catch (Exception error)
        {
            // A refresh must never take Update down with it; the panel keeps its
            // previous rows and says so.
            _title = "Nearby — error, see the log";
            Log.LogError("NearbyPanel refresh failed: " + error);
        }
    }

    private void OnGUI()
    {
        // Sampled here because GUIUtility.keyboardControl only means anything
        // inside OnGUI. Must run before any early return.
        InputGate.SampleGuiFocus();

        if (!Enabled.Value || !_open || !InputGate.ShouldDraw())
        {
            return;
        }

        // Clamped so a stray config value cannot park the panel off-screen with no
        // way to bring it back without editing the file.
        float width = Mathf.Min(_panelWidth.Value, Screen.width - 16f);
        float x = Mathf.Clamp(_marginX.Value, 0f, Mathf.Max(0f, Screen.width - width));
        float y = Mathf.Clamp(_marginY.Value, 0f, Mathf.Max(0f, Screen.height - 64f));

        _view.Draw(
            _rows,
            new Vector2(x, y),
            width,
            _maxVisibleRows.Value,
            _fontSize.Value,
            _title);
    }
}
