# First run

The mod has never been loaded. This is the shortest path from "it builds" to "I have
watched it work", in the order that finds problems fastest. Budget about twenty minutes.

Do this in a **separate r2modman profile**, never the one you play on with other people.
A half-finished plugin loading on every launch of your real profile is exactly the
accident this avoids.

## 1. Make a Dev profile (once)

In r2modman:

1. **Profiles → Create new** → name it `Dev`.
2. Install **BepInExPack Valheim** into it (search it in Online, install). Nothing else is
   needed — this mod has no other dependency.

## 2. Get the mod in

Either way works; the first also tests that the package itself is correct.

**As a package** — r2modman → **Settings → Import local mod** → pick
`build/out/NearbyPanel-0.2.0.zip`.

**Or straight from the build**, which is the loop you want while iterating:

```
dotnet build -p:Deploy=true -p:BepInExPath="%APPDATA%\r2modmanPlus-local\Valheim\profiles\Dev\BepInEx"
```

## 3. Turn the console on

`nearby_dump` needs Valheim's console, which is off by default. Either enable it in the
game's own settings, or add `-console` to the launch parameters in r2modman
(**Settings → Launch parameters**). If F5 does nothing in game, this is why.

## 4. Launch and check it loaded

Start modded from the Dev profile, then look at
`%APPDATA%\r2modmanPlus-local\Valheim\profiles\Dev\BepInEx\LogOutput.log`:

- [ ] `NearbyPanel 0.2.0 loaded (client-side only).`
- [ ] no exception anywhere during startup

To watch it live while you play:

```powershell
Get-Content "$env:APPDATA\r2modmanPlus-local\Valheim\profiles\Dev\BepInEx\LogOutput.log" -Wait -Tail 40
```

## 5. Main menu, before loading anything

- [ ] Press **N**. Nothing should happen, and nothing should appear in the log.

This exercises the "no local player" path, which is where a badly guarded HUD throws.

## 6. In a world, the six things most likely to be wrong

Load any world — a fresh one is fine and faster. Stand somewhere with a few animals.

- [ ] **N opens the panel.** If it does not, check the log for an exception first.
- [ ] **A creature standing level with you reads `0` in ALT, not `+1`.** This was an actual
      bug: the viewer was measured at the feet and the creature at its collider centre.
- [ ] **Turn on the spot. `F` really is straight ahead and `R` really is to your right.**
      Nothing tested this until recently.
- [ ] **STATUS is not clipped.** Find or start taming an animal and look for something like
      `42% Hungry` rather than `42% Hun`. The columns were re-proportioned but never seen.
- [ ] **The ★ column shows `-` for an ordinary creature and `1` for a one-star.** Compare
      against the stars on its health bar.
- [ ] **Type in chat with the panel open.** Pressing `n` must type the letter and must not
      toggle the panel.

## 7. The console dump

- [ ] `nearby_dump` prints a header and rows, and the header lines up with its own columns.
- [ ] The rows match what the panel shows.
- [ ] Back at the main menu, `nearby_dump` prints `no local player yet` and does not throw.

## 8. Scene transitions

Each of these must leave the panel working with no exception in the log:

- [ ] Die, watch the death screen, respawn
- [ ] Log out to the main menu and back into the world
- [ ] Open and close the escape menu, and the inventory

## 9. The multiplayer check

This is the one that matters for other people. Join your normal server **from the Dev
profile** with the mod installed:

- [ ] The connection succeeds
- [ ] No `ErrorVersion`, no "not installed on the server" in the log
- [ ] Nothing changes for anyone else

## 10. Taming, when you have a pen

- [ ] A boar that has not eaten shows no percentage
- [ ] Once it eats, a percentage appears and climbs — slowly: at normal taming durations
      the whole-percent figure only moves every fifteen seconds or so
- [ ] Aggro it: the status changes and the percentage stops
- [ ] The percentage matches the vanilla hover text on the same animal

## If something breaks

Copy the exception from `LogOutput.log` — the whole stack trace — plus the output of
`nearby_dump` at the moment it happened. Those two together are almost always enough to
find it without guessing.

The remaining checks live in [`MANUAL_TESTS.md`](MANUAL_TESTS.md); this file is only the
first pass.
