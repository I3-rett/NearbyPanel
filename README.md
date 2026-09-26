# NearbyPanel

A client-side Valheim mod that lists the creatures near you — nearest first, with
distance, direction, altitude, level and status, including live taming progress.

Status: **early development.** The project builds, the test suite is green and the plugin
loads in game, but the panel itself is not implemented yet. See
[`IMPLEMENTATION_PLAN.md`](IMPLEMENTATION_PLAN.md) for where it stands.

---

## The idea

Running a breeding pen in Valheim means walking up to each animal and reading its hover
text, one at a time, close enough to almost touch it — because taming progress only
appears within interaction range, and only once the animal has actually eaten. With half a
dozen boars in a pen that is a lot of squinting.

NearbyPanel puts the same information in one list: which animals are around, how far and
in which direction, and for each one being tamed, the percentage and whether it is
hungry, frightened, or making progress.

**What it deliberately is not.** The same code, with a wider radius and looser filters,
would be a creature radar that sees through trees and fog. It isn't one: the scan radius
is fixed at 50 m in the source and the list shows tameable creatures only. Neither is a
setting, on purpose — see [ADR 0003](docs/adr/0003-fixed-radius-and-filters.md).

**A limit worth knowing.** Valheim only keeps entities loaded within roughly 64–128 m of
you, and the server only sends ZDOs within about ±160 m. A pen on the other side of the
map is invisible to any client-side mod, this one included.

## Installing

Not yet published. Once there is a release:

1. Install [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
   (r2modman or Gale does this for you).
2. Drop `NearbyPanel.dll` and `NearbyPanel.Core.dll` into
   `BepInEx/plugins/NearbyPanel/`, or install the zip through your mod manager.
3. Launch. `LogOutput.log` should contain `NearbyPanel <version> loaded (client-side only).`

**On your server:** nothing to do. The mod registers no RPC, writes no ZDO and takes part
in no version handshake, so it neither requires the server to have it nor prevents you
joining one that does not. Other players are unaffected and see nothing different.

Settings live in `BepInEx/config/siam.NearbyPanel.cfg`, or in the in-game configuration
manager if you have one installed.

## Building

Needs the .NET SDK (8 or later), and Valheim installed — the project references the game's
own assemblies straight from the Steam folder.

```sh
git clone <this repo>
cd NearbyPanel
dotnet build
```

The build discovers Valheim through the Steam registry key and finds BepInEx in the
r2modman `Default` profile, falling back to the game folder. Override either explicitly if
your layout differs:

```sh
dotnet build -p:GamePath="D:\Games\Valheim" -p:BepInExPath="D:\Games\Valheim\BepInEx"
```

A successful build **deploys straight into your BepInEx profile** (`plugins/NearbyPanel/`).
Point `DeployPath` elsewhere to stop that:

```sh
dotnet build -p:DeployPath="C:\somewhere\else"
```

### Packaging a release

```powershell
pwsh -File build/Package.ps1
```

Builds Release, checks that `package/manifest.json` and `PluginInfo.cs` agree on the
version, checks the icon is exactly 256×256, and writes a verified zip to `build/out/`.
You need to supply `package/icon.png` yourself.

## Testing

### Automated — `dotnet test`

```sh
dotnet test
```

Two kinds of test, both in `tests/NearbyPanel.Tests`:

**Unit tests** over `NearbyPanel.Core`. Core targets netstandard2.0 and cannot reference
UnityEngine or BepInEx, which is what makes it testable at all — the geometry, the
ordering and tie-breaking, the radius cut-off, the filters and the row formatting are all
pure functions over plain data. See [ADR 0004](docs/adr/0004-core-plugin-split.md).

**API guard tests** over the installed game assembly. The plugin binds to Valheim by name
at runtime, so when Iron Gate renames or hides a member, nothing fails at build time — you
find out as a `MissingMethodException` mid-session. These tests read
`assembly_valheim.dll` with Mono.Cecil and assert that every game member the plugin uses
still exists, with the same shape and visibility. One test per member, so a red test
points straight at the call site that needs attention.

If Valheim is not in a standard location, set `VALHEIM_MANAGED`:

```sh
VALHEIM_MANAGED="D:\Games\Valheim\valheim_Data\Managed" dotnet test
```

Without a game assembly the guard tests pass rather than fail, so the rest of the suite
still runs on a machine without Valheim. That means a green suite on such a machine does
**not** prove API compatibility — check that the guards actually ran before trusting them.

### Manual — the rest of it

Anything touching Unity, the network or a live world cannot be unit tested. It is a written
checklist instead of improvisation: [`docs/MANUAL_TESTS.md`](docs/MANUAL_TESTS.md). Work
through all of it before a release. It covers scene transitions (menu, death, respawn,
relog — where a badly parented panel dies), an empty list, a crowded area, input not
leaking into the chat box, multiplayer safety, and an oracle check of every displayed value
against the game's own hover text.

The console command `nearby_dump` writes the current list to `LogOutput.log` using the same
formatter as the panel, so the log is a diffable record of what the panel showed.

### Fast iteration

[ScriptEngine](https://github.com/BepInEx/BepInEx.Debug) (r11.1) reloads a plugin with F6:
point `DeployPath` at `BepInEx/scripts` instead of `BepInEx/plugins`. It does not undo
Harmony patches, so unpatch in `OnDestroy` or you will stack them.
[RuntimeUnityEditor](https://github.com/ManlyMarco/RuntimeUnityEditor) browses the live
scene and is the fastest way to check the scan against reality. DemystifyExceptions, in the
same package as ScriptEngine, makes Mono stack traces readable.

## Contributing

Work in phases, one branch each, as listed in [`TODO.md`](TODO.md). A phase is done when
its boxes are ticked and `dotnet test` is green — not when the code is written.

- **Read [`CONTEXT.md`](CONTEXT.md) first.** It defines the vocabulary the code uses (ZDO,
  ZDO owner, zone, simulation distance, alerted vs aware) with the values verified against
  a specific game version. Guessing at these is how subtle bugs get in.
- **Decisions that constrain later work go in an ADR** under `docs/adr/`, with the context
  that made the decision reasonable. Four exist; read them before arguing with the
  architecture.
- **Keep game types out of Core.** The netstandard2.0 target enforces it, so if you find
  yourself wanting a `UnityEngine.Vector3` in there, the conversion belongs at the adapter
  boundary instead.
- **Add an API guard test** whenever you bind to a new game member. It costs four lines and
  converts a future crash into a build failure.
- **Add a line to `MANUAL_TESTS.md`** for anything you cannot cover automatically.
- **Read a value off the live component, never hardcode it.** `m_tamingTime`,
  `m_fedDuration` and friends are serialized per prefab and are not in the assembly.
- **Never commit game binaries.** `assembly_valheim.dll` and the Unity assemblies are Iron
  Gate's and Unity's, distributed under the Steam EULA, which grants no redistribution
  right. `.gitignore` blocks them and landed in the first commit — keep it that way, and
  never `git add -f` past it.

## Licence

_To be decided._ The mod's own code only; it links against Valheim's assemblies but
redistributes none of them.
