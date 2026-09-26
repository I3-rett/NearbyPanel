# Domain context

The vocabulary this project borrows from Valheim and BepInEx. Everything below was
verified in IL against Valheim 1.0.16 unless marked otherwise.

## Valheim

**ZDO** — the networked record behind a game object: an id, a position, an owner and a
bag of typed values. Replication is per-ZDO. Reading one is a dictionary lookup with no
ownership check, which is why a client can read state for a creature it does not
simulate.

**ZDO owner** — the single peer that simulates a ZDO and writes to it. Writes bump
`DataRevision`; the server forwards any ZDO whose revision advanced to every peer whose
simulation range covers it. So a non-owner sees the owner's values, one network hop late.

**ZDOVars** — the table of stable hashes naming ZDO values. `s_tameTimeLeft` (float,
seconds of taming left), `s_tameLastFeeding` (long, `DateTime` ticks), `s_pregnant`,
`s_level`, `s_alert`.

**Zone** — a 64 × 64 m cell (`ZoneSystem.c_ZoneSize = 64`). The world is a grid of them,
and they are the unit of both generation and replication.

**Simulation distance** — how many zones around a player are synced and instantiated.
Default is near 2 / far 2. In practice: ZDOs arrive within about ±160 m, and game
objects are actually instantiated within Chebyshev 96 m of the centre of the player's
zone, i.e. roughly 64–128 m from the player. The server can cap a client's setting.

**Character** — the component on every creature and player. `GetAllCharacters()` returns
every instantiated one; `GetCharactersInRange` filters by radius. Not a base class for
world objects: berries are `Pickable`, ore deposits are `MineRock5`, and neither is a
`Character`.

**Tameable** — the taming and pet component. Progress is `1 - s_tameTimeLeft /
m_tamingTime`. The timer only advances while the creature has eaten (`!IsHungry()`) and
is not alerted (`!IsAlerted()`), and the owner writes it once every 3 s.
`GetStatusString()` returns the game's own wording: `$hud_tamefrightened`,
`$hud_tamehungry`, `$hud_tamehappy`, `$hud_tameinprogress`.

**Alerted vs aware** — `BaseAI.IsAlerted()` is the `!` icon and freezes taming. The `?`
icon is a separate state (has a target but is not alerted) and does not.

**Hover** — the crosshair readout. A ray from the camera up to 50 m, but the entry is
only accepted when the distance from the player's eye to the hit point is under
`Player.m_maxInteractDistance`. That is why taming percentage is only legible up close.

## BepInEx

**Chainloader** — scans `BepInEx/plugins` and loads each `BaseUnityPlugin`. It will load
a second copy of an assembly found at a second path, which is why build output must
never be deployed wholesale.

**Harmony (HarmonyX)** — runtime patching. Not used by this mod so far; if it becomes
necessary, a private field is reachable through an injected `ref T ___m_field` parameter
without a publicizer.

**Publicized assembly** — a copy of the game assembly with every member made public, for
compile-time access to private members. Not needed here: everything used is already
public. Note that Mono enforces access checks at JIT time unless the assembly is marked
`SkipVerification`, which `AllowUnsafeBlocks` arranges.

**r2modman profile** — the mod set actually loaded lives under
`%APPDATA%\r2modmanPlus-local\Valheim\profiles\<name>\BepInEx`, not in the game folder.
The game folder keeps a residual `BepInEx` directory that nothing reads.
