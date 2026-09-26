# Manual test checklist

Everything that touches Unity, the network or a live world is verified here rather than
in `dotnet test`. Work through the whole list before tagging a release, and note the
game version you tested against.

Tested against Valheim ____________ on ____________ by ____________

## Loading

- [ ] `LogOutput.log` contains `NearbyPanel <version> loaded (client-side only).`
- [ ] No exception anywhere in the log during startup
- [ ] `BepInEx/config/siam.NearbyPanel.cfg` is created on first run
- [ ] The mod's settings appear in the ConfigurationManager window (F1)

## Scene transitions

These are where a panel parented to the wrong object dies. Each step must leave the panel
working, with no exception in the log.

- [ ] Main menu before any world is loaded — toggle key does nothing, no exception
- [ ] Enter a world, open the panel
- [ ] Die, watch the death screen, respawn
- [ ] Log out to the main menu and back into the world
- [ ] Alt-tab away and back

## The list

- [ ] Standing alone in an empty field: panel opens and shows an empty list, not an error
- [ ] Walking towards a creature: it appears and its distance decreases smoothly
- [ ] Walking away past the radius: it disappears
- [ ] Rotating on the spot: DIR changes and F really is straight ahead, R really is right
- [ ] A creature standing level with you reads `0`, not `+1` (feet vs collider centre)
- [ ] A creature clearly above or below: the altitude column carries the right sign
- [ ] Standing in a busy area (40+ creatures): no visible frame drop, list capped as configured
- [ ] Order is stable — rows do not swap positions between refreshes when nothing moves
- [ ] More rows than `Visible rows`: the `+N more` line appears and the count is right
- [ ] The 25-row hard cap: in a big herd the title says 25 even if more are in range

## Layout

These are the ones no automated test can reach, and the columns were re-proportioned
without ever being seen.

- [ ] STATUS is wide enough for `100% Frightened` without clipping
- [ ] A long creature name does not visually collide with DIST
- [ ] Font size 8 and font size 32 both render without rows overlapping or text clipping
- [ ] Panel width at its minimum (240) and maximum (1200) both look sane
- [ ] Numeric columns line up with their headings

## Oracle check against vanilla

The game's own hover text is the reference. Pick three creatures and compare.

- [ ] Name in the panel matches the vanilla hover name
- [ ] Star level matches the stars on the health bar
- [ ] For a tameable: status wording matches the vanilla hover text (frightened, hungry,
      happy, in progress)
- [ ] For an animal being tamed: percentage matches the vanilla hover percentage, allowing
      for the 3 s write interval
- [ ] `nearby_dump` in the console writes the same rows the panel shows

## Taming, in a pen

- [ ] A boar that has not eaten shows no percentage (vanilla shows "wild")
- [ ] Once it eats, a percentage appears and climbs
- [ ] Aggro it: status becomes frightened and the percentage stops climbing
- [ ] Let it calm down: the percentage resumes
- [ ] An animal being tamed by another player shows progress too

## Input

- [ ] Toggle key opens and closes the panel
- [ ] Pressing the toggle key while the chat box has focus types the character and does
      **not** toggle the panel
- [ ] Same with the console open
- [ ] Same while renaming a tamed animal
- [ ] Same while typing in the configuration manager's search box
- [ ] Panel does not swallow clicks meant for the game
- [ ] `nearby_dump` requires the console, which is off until enabled in the game's settings

## Multiplayer safety

The point of the mod being client-only. Run these against the shared server.

- [ ] Joining the server succeeds with the mod installed and the server without it
- [ ] No `ErrorVersion`, no "not installed on the server" in the log
- [ ] Another player without the mod is unaffected and sees nothing unusual
- [ ] Nothing is written to any ZDO: leave the panel open for ten minutes, then confirm
      tamed animals and the world are unchanged after a server restart

## Packaging

- [ ] Install the built zip through r2modman into a fresh profile
- [ ] Mod appears with the right name, version and icon
- [ ] Launch and confirm the load line in the log
- [ ] Uninstall and confirm the game still starts clean
