using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using NearbyPanel.Core;
using UnityEngine;

namespace NearbyPanel;

/// <summary>
/// Entry point. Client-side only: this plugin reads local component and ZDO state
/// and draws a panel. It registers no RPC, writes no ZDO and takes part in no
/// version handshake, so it never affects joining a server that does not have it.
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
    private Vec3 _viewer;
    private Vec3 _forward;

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
            "Shows and hides the panel. Ignored while typing in chat or the console.");

        _openOnStart = Config.Bind(
            "General",
            "Open on start",
            false,
            "Whether the panel is already showing when you load into a world.");

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
            420f,
            new ConfigDescription("Panel width in pixels.", new AcceptableValueRange<float>(240f, 1200f)));

        _marginX = Config.Bind(
            "Panel",
            "Margin X",
            12f,
            "Distance from the left edge of the screen, in pixels.");

        _marginY = Config.Bind(
            "Panel",
            "Margin Y",
            120f,
            "Distance from the top edge of the screen, in pixels.");

        _open = _openOnStart.Value;

        // Constructing the command registers it with the terminal, which keeps its
        // own static table, so this is safe to do before any world is loaded.
        DumpCommand.Register(_scanner);

        Log.LogInfo(PluginInfo.Name + " " + PluginInfo.Version + " loaded (client-side only).");
    }

    private void Update()
    {
        // Leaving a world invalidates the remembered taming state, because instance
        // ids only mean anything while the objects they name are alive.
        bool hasPlayer = Player.m_localPlayerExists && Player.m_localPlayer != null;
        if (_hadPlayer && !hasPlayer)
        {
            _scanner.Reset();
            _rows.Clear();
        }

        _hadPlayer = hasPlayer;

        if (!Enabled.Value)
        {
            _open = false;
            return;
        }

        if (_toggleKey.Value.IsDown() && InputGate.AcceptsHotkey())
        {
            _open = !_open;
            _sinceRefresh = RefreshSeconds; // refresh on the frame it opens
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
        IReadOnlyList<NearbyEntity> entities = _scanner.Scan(NearbyList.IsTameable);
        _viewer = _scanner.Viewer;
        _forward = _scanner.Forward;

        _rows.Clear();
        foreach (NearbyEntity entity in entities)
        {
            _rows.Add(RowFormatter.Cells(entity, _viewer, _forward));
        }
    }

    private void OnGUI()
    {
        if (!Enabled.Value || !_open || !InputGate.ShouldDraw())
        {
            return;
        }

        string title = _rows.Count == 0
            ? "Nearby — nothing within " + Tuning.ScanRadius.ToString("0") + " m"
            : "Nearby — " + _rows.Count + " within " + Tuning.ScanRadius.ToString("0") + " m";

        _view.Draw(
            _rows,
            new Vector2(_marginX.Value, _marginY.Value),
            _panelWidth.Value,
            _maxVisibleRows.Value,
            _fontSize.Value,
            title);
    }
}
