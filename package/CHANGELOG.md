# Changelog

## 0.2.0 — unreleased, not yet tested in game

First working version. Everything below builds with no warnings and is covered by
95 automated tests, but none of it has been seen running yet.

- Panel listing tameable creatures within 50 m, toggled with **N**
- Per row: name, distance, compass direction, altitude, level, and for an animal being
  tamed, the percentage and the game's own status wording
- Taming progress read from the network record, so an animal a friend is taming reports
  too; smoothed between the owner's three-second writes, and held while the animal is
  hungry or frightened
- `nearby_dump` console command, `nearby_dump all` to list every creature
- Panel is read-only: it takes no click and never touches the cursor
- Hotkey ignored while typing in chat, the console, a rename box, a menu, the inventory,
  or while placing a building piece
- Nothing is scanned while the panel is closed

## 0.1.0

Scaffolding only. Plugin loaded and logged its version.
