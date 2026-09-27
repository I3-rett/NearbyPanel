# ADR 0007 — List all creatures, with a text filter

**Status:** accepted

Supersedes the filter half of [ADR 0003](0003-fixed-radius-and-filters.md). The radius
half of that decision stands unchanged.

## Context

ADR 0003 fixed the list to tameable creatures, on the argument that a wider filter turns
the same code into a creature radar that sees through trees and fog.

The first session in game made the cost of that visible. Valheim has five tameable
species. Outside a pen the panel is simply empty, which the owner discovered by meeting a
greydwarf and seeing nothing at all. A tool that is blank during everything except animal
husbandry is a tool you stop opening.

The owner weighed the radar objection — twice, having had it put to them twice — and
decided the utility is worth it on a private server among friends who can be told what the
mod does. That is their call to make: it is their server, their agreement, and the
information is already on their client.

## Decision

The list shows **every creature** within the radius, not only tameable ones.

A free-text filter narrows it by name, case-insensitively and on substrings, set with
`nearby_filter <text>` in the console or through the `Name filter` config entry. Empty
means everything. The filter is a convenience, not a safety mechanism.

**Other players are excluded**, unconditionally and not configurably. Their position and
distance through walls is the part of a radar that affects other people rather than the
person running it, and nobody asked for it.

**The radius stays a 50 m constant**, exactly as ADR 0003 decided. That decision was about
reach, and reach is what separates "roughly what I could see by walking around" from
surveillance. Nothing here touches it.

## Consequences

- The panel is now useful while exploring, and it is a short-range creature radar. Both
  are true; the README says so plainly rather than describing it as a husbandry tool.
- The list gets long in a forest. The 25-row cap and the configurable visible-row count
  carry more weight than they did, and the `+N more` line will be seen often.
- There is no text box in the panel, because ADR 0005 keeps it inert — no clicks, no
  keyboard focus. The console and the config file are the input surface.
- ADR 0003's amendment about `nearby_dump all` is now moot: there is no longer a wider
  view for an argument to unlock, so the dump simply shows what the panel shows.
- If this is ever shared beyond that private server, the README's description is the thing
  to check first — not the code.
