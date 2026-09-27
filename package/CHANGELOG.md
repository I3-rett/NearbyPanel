# Changelog

## 0.3.0 — unreleased

First version seen running. The plugin loads, the panel opens and draws, and no exception
appears in a session. Everything else in the checklist is still unwalked.

- Lists **every creature** in range, not only tameable ones (ADR 0007). Other players are
  never listed, and the 50 m radius is unchanged.
- Text filter by name: `nearby_filter <text>` in the console, or the `Name filter` setting.
  Empty shows everything.
- A rejected hotkey now says which check swallowed it, instead of doing nothing silently.

### Fixed

- The toggle key did nothing in game. `GUIUtility.keyboardControl` was read from `Update`,
  where it is not meaningful, and could block the key permanently. It is now sampled inside
  `OnGUI`, where it is valid.

## 0.2.0 — never run in game

First complete version. It builds with no warnings and 111 automated tests pass, but the
mod has not been loaded in game once. The panel, the console command and the hotkey gating
have **no** automated coverage — they live in the plugin assembly, which cannot be unit
tested — so treat this as unverified.

- Panel listing tameable creatures within 50 m, toggled with **N**
- Per row: name, ground distance, heading relative to where you face, altitude, star
  rating, and for an animal being tamed, the percentage and the game's own status wording
- Taming progress read from the network record, so an animal a friend is taming reports too
- Your own tamed animals appear in the list, by their given name
- `nearby_dump` console command, writing the same rows to the log
- Panel is read-only: it takes no click and never touches the cursor
- Hotkey ignored while typing in chat, the console, a rename box, a menu, the inventory,
  any IMGUI text field, or while placing a building piece
- Nothing is scanned while the panel is closed
- A creature that cannot be read is skipped rather than taking the panel down

## 0.1.0

Scaffolding only. Plugin loaded and logged its version.
