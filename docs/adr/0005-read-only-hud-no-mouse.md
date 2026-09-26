# ADR 0005 — The panel is a read-only HUD: no cursor, no drag, no scroll

**Status:** accepted

Supersedes the "draggable, scrollable" wording in the original phase 4 plan.

## Context

The plan said the panel would be a draggable, scrollable window. Writing it made the
cost clear.

Dragging means `GUI.DragWindow`, which needs the cursor, which means taking the cursor
away from the game and giving it back correctly on every path out — closing the panel,
dying, opening the inventory, alt-tabbing, the escape menu. Scrolling means the mouse
wheel, with the same problem. Without Jotunn's `GUIManager.BlockInput` there is no
framework doing this: shudnal's ConfigurationManager spends thirteen Harmony patch classes
plus a set of cursor save/restore helpers on exactly this problem, and Valheim Foresight
takes the cruder route of disabling the `Player` and `GameCamera` components while its
window is open.

Against that, ask what the panel is for. It lists at most a couple of dozen rows within
50 m, refreshed four times a second. There is nothing to click, nothing to select and
nothing to reorder. With twelve visible rows by default, there is usually nothing to
scroll either.

## Decision

The panel is inert. It draws with `GUI.Box` and `GUI.Label`, takes no click, never
touches `Cursor.visible` or `Cursor.lockState`, and is not draggable. Position and size
are configuration values rather than something dragged with the mouse. When there are
more rows than fit, the list is cut short and a `+N more` line says how many were hidden.

`InputGate` covers the keyboard side, since a hotkey still must not fire while the player
is typing: `Chat.instance.HasFocus()`, `Console.IsVisible()`, `TextInput.IsVisible()`,
`Menu.IsVisible()`, `InventoryGui.IsVisible()`, and `Player.InPlaceMode()`.

## Consequences

- Zero risk of the mod stealing a click, swallowing a keypress or leaving the cursor in
  the wrong state. That whole class of bug does not exist here.
- Moving the panel means editing two config values, which the in-game configuration
  manager makes livable. Less convenient than dragging; far less code.
- A long list is truncated rather than scrolled. If that turns out to be annoying in
  practice, the honest next step is a keyboard page-down binding, not a mouse.
- Cells are drawn at fixed pixel offsets rather than padded with spaces, because Unity's
  built-in GUI font is proportional. `RowFormatter.Cells` returns the values and each
  consumer lays them out: fixed-width for the log, positioned for the panel.
- If the panel ever does need real interaction, revisit ADR 0001 at the same time —
  depending on Jotunn for `BlockInput` becomes the cheaper option at that point.
