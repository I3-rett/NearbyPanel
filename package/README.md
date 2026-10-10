# NearbyPanel

Lists the creatures and fish around you — how far, which way, how high, how many stars, what they
have noticed, how far along any animal you are taming or raising is, and why a tamed animal
is or is not breeding. Press **N**.

**Client-side only. The server does not need it, and it will not stop you joining a server
that does not have it.** Players without the mod are unaffected and see nothing different.

![Lox calves growing under a tower, each with its growth percentage](https://raw.githubusercontent.com/I3-rett/NearbyPanel/main/docs/images/lox-pen.jpg)

![A boar pen at night: piglets growing, the parents, and a greyling further out](https://raw.githubusercontent.com/I3-rett/NearbyPanel/main/docs/images/boar-pen.jpg)

## Features

- Every creature within 50 m, nearest first, refreshed four times a second; animals you
  have named come first
- Distance along the ground, so it is the number you actually walk
- Direction as an arrow from where you are looking, or in degrees, relative letters, or
  world compass points that do not turn when you do
- Star rating, and altitude relative to you
- What each creature has noticed: nothing, something, or you specifically when the game
  can confirm it
- A red row for anything hostile that has been alerted — the deathsquito you did not see
  take aggro
- Live taming progress, read from the same record the game uses, so an animal a friend is
  taming reports its progress too, along with whether it is hungry or frightened
- Growth of young animals, and taming, growth and pregnancy as a percentage, a time left or
  both: `62% grown · 12 min`. Taming time accounts for Brew of Animal Whispers
- Breeding: `Love 2/4`, `Pregnant`, or what is stopping it — `Crowded 5/4 · Lox Calf 18.4 m`,
  `Partner pregnant`, `Partner hungry`, `No partner` — with each species' own ranges
- Column widths are settings, so the panel fits your font and screen
- Filter the list by name with `nearby_filter boar`, or clear it with `nearby_filter`
- `nearby_dump` writes the current list to the log, which is useful in a bug report
- Other players are never listed

The panel takes no clicks and never touches your cursor, so it cannot interfere with the
game.

## Scope

It lists every creature within 50 m, which means it shows you things through trees and
fog that you could not see yourself. Decide whether that suits the people you play with.
Fish count too: they show their size in the ★ column and, under STATUS, the baits the
species bites on.

Two limits are fixed in the source rather than left as settings: the **50 m radius**,
because reach is what separates this from surveillance and a limit you can slide is not a
limit, and **other players are never listed**. The game only keeps creatures loaded within
roughly 64-128 m of you, so nothing beyond that is visible to any client-side mod anyway.

## Configuration

A config file is generated on first launch at `BepInEx/config/I3_rett.NearbyPanel.cfg`.
Edit it with a text editor, or in game with Configuration Manager. The settings most
people want are the toggle key, the screen corner the panel is pinned to, and the
direction format.

## Console commands

Both need Valheim's console, which is off by default: enable it in the game's settings, or
add `-console` to the launch parameters (r2modman: **Settings → Launch parameters**). Then
press **F5** in game.

- `nearby_filter <text>` shows only creatures whose name contains the text, e.g.
  `nearby_filter lox`. With no argument it clears the filter. Also a setting.
- `nearby_dump` writes the list as the panel shows it, same filter, to the console and to
  `BepInEx/LogOutput.log`. Under each animal that breeds it adds what the breeding checks
  counted: its ranges, and every animal considered with its 3-D and ground distance, height
  and whether it is ready to mate. That is the place to look when a pen reads `Crowded` or
  `No partner` and you cannot see why.

Neither marks your character as having used cheats. When reporting a bug, attach
`LogOutput.log` with a `nearby_dump` taken at the moment it happened.

## Compatibility

- No Harmony patches, no RPC, and nothing is ever written to the world, so removing the
  mod cannot damage a save
- Running `nearby_dump` does not mark your character as having used cheats
- Tested against Valheim 1.0.16 with BepInEx 5.4.2351

## Installation

A mod manager does this for you. Manually: install BepInExPack Valheim, then extract
`NearbyPanel.dll` and `NearbyPanel.Core.dll` into `BepInEx/plugins/NearbyPanel/`. The
archive is also attached to each release on GitHub.

## Source

MIT licensed. Code and issues: https://github.com/I3-rett/NearbyPanel
