# ADR 0003 — Radius and filters are constants, not settings

**Status:** accepted for the radius; the filter half is superseded by [ADR 0007](0007-list-all-creatures.md)

## Context

The same code that lists nearby tameable animals at 40 m is, at 200 m with the faction
filters opened up, a creature radar: it reveals what is behind trees, hills and fog. The
data is all legitimately on the client, so nothing technical distinguishes the two. Only
the radius and the filter do.

This mod is used on a small server among friends who agreed not to run that kind of
overlay. An agreement that depends on everyone leaving a slider alone is not much of an
agreement; BetterMap's creature radius defaults to 100 m and goes to 200 m, and its author
had to add server-side config locking to make the limit mean anything.

## Decision

`ScanRadius` and the default filter are `const` in the source. They are not
`ConfigEntry` values and do not appear in the configuration manager window.

Starting values: radius 50 m, default filter `NearbyList.IsTameable`.

## Consequences

- Nobody, including the author, can widen the radius mid-session. Changing it means
  editing the source, rebuilding, and shipping a new version — visible to everyone.
- Cosmetic settings (the toggle key, panel position, row count, master switch) stay in
  `ConfigEntry` where they belong.
- If a filter for wild non-tameable creatures is ever added, it ships off by default and
  is documented in the README as what it is.
- This closes the door on "just make it configurable" as a response to a feature request.
  That is the point.

## Amendment, 2026-09-27

The `nearby_dump` console command briefly took an `all` argument that dropped the filter
and listed every creature in range — players included — by name, distance and bearing. It
was added for debugging and it is exactly the thing this ADR exists to prevent: a console
argument is a *weaker* gate than the slider the decision above already rejects, not a
stronger one, and it shipped undocumented in the main README.

It has been removed. `nearby_dump` now applies the same filter as the panel. If a
debugging view of everything nearby is ever needed again, it belongs behind the game's own
cheat gate (`isCheat: true` on the command), not behind an argument.
