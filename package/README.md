# NearbyPanel

Press **N** for a list of the creatures near you — nearest first, with distance,
direction, altitude, level, and live taming progress for animals you are taming.

Built for keeping an eye on a breeding pen without walking up to each animal and
squinting at the hover text one at a time.

## Client-side only

Install it on your own game. It does not need to be on the server, it does not stop you
joining a server that does not have it, and other players are unaffected. It reads state
your client already holds, registers no RPC and writes nothing.

## What it shows

| Column | |
|---|---|
| NAME | the creature, or its given name if you have named it |
| DIST | ground distance in metres, height ignored |
| DIR | compass point relative to the way you are facing |
| ALT | how far above or below you it is |
| LVL | creature level |
| STATUS | taming percentage and the game's own wording: hungry, frightened, in progress, happy |

Taming progress comes from the same record the game uses, so an animal a friend is taming
shows its progress too. It is smoothed between updates, and stops climbing when the animal
is hungry or frightened — because that is when the game itself stops counting.

## Scope

The scan radius is fixed at 50 m and the list shows tameable creatures. Both are constants
in the source rather than settings: this is a tool for watching your animals, not a
creature radar, and a limit you can slide is not a limit.

The game only keeps entities loaded within roughly 64–128 m of you, so anything further
away is invisible to any client-side mod, this one included.

## Settings

Toggle key, panel position and size, visible rows and font size. Use the in-game
configuration manager if you have one, or edit `BepInEx/config/siam.NearbyPanel.cfg`.

The panel is deliberately inert: it takes no clicks and never grabs your cursor, so it
cannot interfere with the game. Move it with the margin settings rather than by dragging.

## Console

`nearby_dump` writes the current list to the log, `nearby_dump all` lists every creature
rather than only tameable ones. Handy for reporting a problem.
