# Changelog

## 1.1.1

- **Names with colour tags.** The Hildir quest bosses (Brenna, Geirrhafa, Zil, Thungr) and
  Lord Reto are localized by the game as `<color=orange>…</color>`. The panel, which draws
  without rich text, showed the tag cut by the column width. Tags are now removed from
  names and status text before display.

## 1.1.0

Built around watching a breeding pen.

- **Breeding.** A tamed adult that breeds shows why it is or is not: `Pregnant 25% · 45 s`,
  `Crowded 5/4 · Lox Calf 18.4 m` with the nearest animal in the way, `Partner pregnant`,
  `Partner hungry`, `No partner`, or `Love 2/4`. Love points and pregnancy are read from
  the game's record; crowding and the partner check are recomputed with the game's own rule
  and each species' own ranges (a lox counts within 20 m, not 10)
- **Growth.** A young animal shows how far it has grown: `62% grown`
- **Time left.** Taming, growth and pregnancy can read as a percentage, a time left, or both
  (`62% grown · 12 min`, the new default). Taming time says `fed`, since it only runs down
  while the animal has eaten, and counts Brew of Animal Whispers on players near the animal
- **Arrows.** A fourth direction format, eight arrows from where you are looking, now the
  default
- **Named animals first**, each group nearest first, so the parents are never pushed off
  the bottom of the list. A setting, on by default
- **Column widths** are settings, in pixels; STATUS takes the rest. The panel now defaults
  to the bottom-right corner
- The awareness column is headed `ALERT`, and no stars is blank rather than `-`
- The list counts every creature in range; it used to stop at 25
- `nearby_dump` lists, under each breeding animal, every animal the checks counted and how
  far away it is

Existing settings are kept: new defaults only apply to a fresh config.

## 1.0.0

First public release.

- A panel listing every creature within 50 m, nearest first, toggled with **N**
- Ground distance, altitude, star rating, and direction in one of three formats: degrees
  from where you are looking, relative letters, or world compass points
- What each creature has noticed, and a red row for anything hostile that has been alerted
- Live taming progress, read from the same record the game uses, so an animal a friend is
  taming reports its progress too
- Text filter by name: `nearby_filter boar`
- `nearby_dump` writes the current list to the log
- Settings for the toggle key, the screen corner, the size and the direction format,
  editable in game with Configuration Manager
- Client-side only: no RPC, no Harmony patches, and nothing written to the world

Tested against Valheim 1.0.16 with BepInEx 5.4.2351, including joining a dedicated server
that does not have the mod.

Two things are deliberately not configurable: the 50 m radius, and the exclusion of other
players from the list.
