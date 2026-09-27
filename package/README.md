# NearbyPanel

Press **N** for a list of the creatures near you — nearest first, with distance,
direction, altitude, star rating, and live taming progress for animals you are taming.

## Client-side only

Install it on your own game. It does not need to be on the server, it does not stop you
joining a server that does not have it, and other players are unaffected. It reads state
your client already holds, registers no RPC and writes nothing.

## What it shows

| Column | |
|---|---|
| NAME | the creature, or its given name if you have named it |
| DIST | ground distance in metres, height ignored |
| DIR | heading **relative to the way you are facing**: F ahead, R right, B behind, L left, and the diagonals |
| ALT | how far above or below you it is |
| ★ | star rating, `-` for an ordinary creature |
| STATUS | for tameable animals: the taming percentage and the game's own wording — hungry, frightened, in progress, happy |

Taming progress comes from the same record the game uses, so an animal a friend is taming
shows its progress too. It is reported exactly as stored, with no smoothing — at normal
taming durations the whole-percent figure moves every fifteen seconds or so.

## Filtering

The list shows every creature in range, which in a forest is a lot. Narrow it by name:

```
nearby_filter boar     in the console
nearby_filter          with no argument, to clear it
```

It matches anywhere in the name and ignores case, and it works on the given names of your
tamed animals too. The same value is the `Name filter` setting, so an in-game
configuration manager can set it as well.

`nearby_dump` writes the current list to the log with the same filter and formatting as
the panel — handy for reporting a problem.

## Scope, stated plainly

This lists every creature within 50 m, so it is a short-range creature radar: it shows you
things through trees and fog that you could not see yourself. Decide for yourself whether
that suits the group you play with.

Two things are fixed in the source and cannot be widened from the config:

- **The radius is 50 m.** Reach is what separates this from surveillance.
- **Other players are never listed.** Their position through walls is the part that
  affects someone other than you.

The game only keeps entities loaded within roughly 64–128 m of you, so nothing beyond that
is visible to any client-side mod anyway.

## Settings

Toggle key, name filter, panel position and width, visible rows and font size, in
`BepInEx/config/I3_rett.NearbyPanel.cfg`.

The panel is deliberately inert: it takes no clicks and never grabs your cursor, so it
cannot interfere with the game. Move it with the margin settings rather than by dragging.

## Source and licence

MIT. Code, issues and the full design notes: https://github.com/I3-rett/NearbyPanel
