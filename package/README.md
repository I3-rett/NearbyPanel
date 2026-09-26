# NearbyPanel

Opens a panel listing the creatures near you, nearest first, with distance, direction,
altitude, level and status — including taming progress for animals you are taming.

Built for keeping an eye on a breeding pen without having to walk up to each animal and
squint at the hover text.

## Client-side only

Install it on your own game. It does not need to be on the server, it does not stop you
joining a server that does not have it, and other players are unaffected. It reads state
your client already holds, registers no RPC and writes nothing.

## Scope

The scan radius is fixed at 50 m and the list shows tameable creatures. Both are
constants in the source rather than settings — this is a tool for watching your animals,
not a creature radar, and a limit you can slide isn't a limit.

The game only keeps entities loaded within roughly 64–128 m of you, so anything further
away is invisible to any client-side mod, this one included.

## Settings

Toggle key, panel position and row count are configurable. Use the in-game configuration
manager (F1) or edit `BepInEx/config/siam.NearbyPanel.cfg`.

## Console

`nearby_dump` writes the current list to `LogOutput.log` — handy for reporting a problem.
