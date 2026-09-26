# ADR 0004 — Split a Unity-free Core out of the plugin

**Status:** accepted

## Context

A BepInEx plugin cannot be unit tested as a whole: its code needs a live Unity scene, a
`ZNet` connection and a loaded world. The usual outcome is a mod with no tests at all,
where every change is verified by launching the game and looking.

But most of the interesting logic is not Unity-specific: distance and bearing, ordering,
tie-breaking, the radius cut-off, the row layout, the percentage rounding rule. All of it
is pure.

## Decision

Two assemblies:

- `NearbyPanel.Core`, targeting netstandard2.0 and referencing neither UnityEngine nor
  BepInEx. It defines its own `Vec3` rather than using `UnityEngine.Vector3`.
- `NearbyPanel.Plugin`, targeting net48, holding the scan, the component and ZDO reads,
  the IMGUI panel and the config — and nothing else.

The conversion from game types to `NearbyEntity` happens at that boundary and nowhere
else.

## Consequences

- About 40% of the shipped code is under `dotnet test` (Core is ~260 of ~660 non-comment
  lines). Without the split it would be 0%, which is the point — but the honest figure is
  40%, not the 60% this ADR originally claimed. Some of the remainder is decisions rather
  than glue and could still move across; see the contributing notes.
- The netstandard2.0 target enforces the rule mechanically: a stray `using UnityEngine`
  in Core will not compile, so the discipline cannot erode quietly.
- Records need an `IsExternalInit` polyfill on netstandard2.0. One file, `Polyfills.cs`.
- Two DLLs ship instead of one. The post-build step copies both. An alternative would be
  ILMerge, as BetterMap does for ServerSync, but that adds a build dependency to save one
  file — not worth it.
- `RowFormatter` is shared by the panel and the `nearby_dump` console command, so the text
  in the log is exactly the text on screen. That makes the log a usable oracle.
