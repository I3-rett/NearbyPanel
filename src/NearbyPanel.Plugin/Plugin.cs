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

    /// <summary>The DIR format the panel is using, so the dump matches it.</summary>
    internal static DirectionFormat DirectionFormat =>
        _directionFormat == null ? Core.DirectionFormat.Arrow : _directionFormat.Value;

    /// <summary>The STATUS progress format the panel is using, so the dump matches it.</summary>
    internal static ProgressFormat ProgressFormat =>
        _progressFormat == null ? Core.ProgressFormat.Both : _progressFormat.Value;

    private static ConfigEntry<ProgressFormat> _progressFormat = null!;

    /// <summary>Whether named animals head the list, so the dump orders as the panel does.</summary>
    internal static bool NamedFirst => _namedFirst == null || _namedFirst.Value;

    private static ConfigEntry<KeyboardShortcut> _toggleKey = null!;
    private static ConfigEntry<bool> _namedFirst = null!;
    private static ConfigEntry<bool> _openOnStart = null!;
    private static ConfigEntry<PanelAnchor> _anchor = null!;
    private static ConfigEntry<DirectionFormat> _directionFormat = null!;
    private static ConfigEntry<int> _maxVisibleRows = null!;
    private static ConfigEntry<int> _fontSize = null!;

    /// <summary>One width per column before STATUS, which takes the rest.</summary>
    private static readonly ConfigEntry<float>[] ColumnWidthSettings =
        new ConfigEntry<float>[RowFormatter.ColumnNames.Length - 1];

    // Refilled from the settings every draw; reused so that allocates nothing.
    private readonly float[] _columnWidths = new float[RowFormatter.ColumnNames.Length - 1];
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

        // Order counts down so a configuration manager lists these the way they are
        // written here rather than alphabetically.
        Enabled = Config.Bind(
            "General",
            "Enabled",
            true,
            new ConfigDescription(
                "Master switch. Turning this off hides the panel and stops the scan.",
                null,
                new ConfigurationManagerAttributes { Order = 100 }));

        _toggleKey = Config.Bind(
            "General",
            "Toggle key",
            new KeyboardShortcut(KeyCode.N),
            new ConfigDescription(
                "Shows and hides the panel. Ignored while typing in chat or the console. "
                + "A shortcut with no modifier will not fire while a modifier is held.",
                null,
                new ConfigurationManagerAttributes { Order = 90 }));

        FilterState.Bind(Config.Bind(
            "General",
            "Name filter",
            string.Empty,
            new ConfigDescription(
                "Only list creatures whose name contains this text, case-insensitively. "
                + "Empty lists everything. Also settable in game with: nearby_filter <text>",
                null,
                new ConfigurationManagerAttributes { Order = 80 })));

        _openOnStart = Config.Bind(
            "General",
            "Open on start",
            false,
            new ConfigDescription(
                "Whether the panel is already showing when you load into a world.",
                null,
                new ConfigurationManagerAttributes { Order = 70 }));

        _namedFirst = Config.Bind(
            "General",
            "Named first",
            true,
            new ConfigDescription(
                "List animals you have named before the rest, each group nearest first. "
                + "A named animal is then never pushed off the bottom of the list by a "
                + "crowd of unnamed ones.",
                null,
                new ConfigurationManagerAttributes { Order = 60 }));

        _directionFormat = Config.Bind(
            "Panel",
            "Direction format",
            DirectionFormat.Arrow,
            new ConfigDescription(
                "How the DIR column reads. Degrees: angle from where you are looking, "
                + "0 ahead, 90 right, -90 left, 180 behind. Relative: the same as letters, "
                + "F ahead, R right, B behind. Compass: world direction, N NE E SE S SW W NW, "
                + "which does not change as you turn. Arrow: the same as Relative, drawn as "
                + "eight arrows.",
                null,
                new ConfigurationManagerAttributes { Order = 95 }));

        _progressFormat = Config.Bind(
            "Panel",
            "Progress format",
            ProgressFormat.Both,
            new ConfigDescription(
                "How taming, growth and pregnancy read in STATUS. Percent: how far along, 42%. "
                + "Time: how long is left, 4 min. Taming time only counts down while the "
                + "animal is fed, so it reads '4 min fed'. Both: 42% · 4 min fed.",
                null,
                new ConfigurationManagerAttributes { Order = 94 }));

        _anchor = Config.Bind(
            "Panel",
            "Anchor",
            PanelAnchor.BottomRight,
            new ConfigDescription(
                "Which corner of the screen the panel is pinned to. The margins below are "
                + "measured from that corner.",
                null,
                new ConfigurationManagerAttributes { Order = 100 }));

        _marginX = Config.Bind(
            "Panel",
            "Margin X",
            12f,
            new ConfigDescription(
                "Horizontal distance from the anchored corner, in pixels.",
                new AcceptableValueRange<float>(0f, 600f),
                new ConfigurationManagerAttributes { Order = 90 }));

        _marginY = Config.Bind(
            "Panel",
            "Margin Y",
            120f,
            new ConfigDescription(
                "Vertical distance from the anchored corner, in pixels.",
                new AcceptableValueRange<float>(0f, 600f),
                new ConfigurationManagerAttributes { Order = 80 }));

        _panelWidth = Config.Bind(
            "Panel",
            "Width",
            700f,
            new ConfigDescription(
                "Panel width in pixels.",
                new AcceptableValueRange<float>(240f, 1200f),
                new ConfigurationManagerAttributes { Order = 70 }));

        _maxVisibleRows = Config.Bind(
            "Panel",
            "Visible rows",
            12,
            new ConfigDescription(
                "Rows drawn before the list is cut short with a count of the rest.",
                new AcceptableValueRange<int>(1, Tuning.MaxRows),
                new ConfigurationManagerAttributes { Order = 60 }));

        _fontSize = Config.Bind(
            "Panel",
            "Font size",
            20,
            new ConfigDescription(
                "Text size in the panel.",
                new AcceptableValueRange<int>(8, 32),
                new ConfigurationManagerAttributes { Order = 50 }));

        BindColumnWidths();

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
            IReadOnlyList<NearbyEntity> entities = _scanner.Scan(FilterState.Predicate(), NamedFirst);

            _rows.Clear();
            foreach (NearbyEntity entity in entities)
            {
                _rows.Add(RowFormatter.Cells(
                    entity, _scanner.Viewer, _scanner.Forward, _directionFormat.Value, _progressFormat.Value));
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

        PanelRect placement = PanelLayout.Place(
            _anchor.Value,
            _marginX.Value,
            _marginY.Value,
            _panelWidth.Value,
            PanelLayout.Height(_rows.Count, _maxVisibleRows.Value, _fontSize.Value),
            Screen.width,
            Screen.height);

        for (int i = 0; i < _columnWidths.Length; i++)
        {
            _columnWidths[i] = ColumnWidthSettings[i].Value;
        }

        _view.Draw(_rows, placement, _maxVisibleRows.Value, _fontSize.Value, _title, _columnWidths);
    }

    /// <summary>
    /// A width in pixels for every column but STATUS, which takes what is left of the
    /// panel. Defaults were calibrated in game at font size 20; they do not scale
    /// with the font, so a larger font needs wider columns. 0 hides a column.
    /// </summary>
    private void BindColumnWidths()
    {
        float[] defaults = { 120f, 64f, 64f, 64f, 32f, 80f };

        // ASCII keys: the cfg file is edited by hand too, and "★ width" is hard to type.
        string[] names = { "NAME", "DIST", "DIR", "ALT", "Stars", "ALERT" };

        for (int i = 0; i < ColumnWidthSettings.Length; i++)
        {
            ColumnWidthSettings[i] = Config.Bind(
                "Columns",
                names[i] + " width",
                defaults[i],
                new ConfigDescription(
                    $"Width of the {RowFormatter.ColumnNames[i]} column in pixels. STATUS takes whatever is left "
                    + "of the panel width. 0 hides the column.",
                    new AcceptableValueRange<float>(0f, 600f),
                    new ConfigurationManagerAttributes { Order = 100 - i }));
        }
    }
}
