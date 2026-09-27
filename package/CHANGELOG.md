# Changelog

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
