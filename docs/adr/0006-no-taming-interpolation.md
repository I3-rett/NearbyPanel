# ADR 0006 — No smoothing of taming progress

**Status:** accepted

Reverses a decision taken during phase 5 and removes the code that implemented it.

## Context

The peer that owns a creature writes its remaining taming time once every three seconds
(`Tameable.Awake` → `InvokeRepeating("TamingUpdate", 3f, 3f)`, which decrements by three
seconds each time). Read raw, a percentage therefore sits still and then steps, which
looked like something worth smoothing. A `TamingTracker` was built to do exactly that:
remember the last value per creature, extrapolate between writes while taming was running,
hold while the animal was hungry or frightened, cap the prediction at one write interval.
140 lines, twelve tests, and the tests read like a specification.

Then the numbers were checked against the game rather than against the abstraction.
`m_tamingTime` is on the order of 1 800 seconds for a boar. Three seconds of taming is
0.17% — six times below the resolution of the whole-percent figure the panel displays. The
raw percentage does not step every three seconds; it ticks roughly every eighteen. **There
was no visible stepping to smooth.**

Worse, extrapolation makes the panel lead the truth. The record is already decremented for
the interval just elapsed, so predicting a further `elapsed` seconds shows progress that
has not happened. The panel could read 100% up to three seconds before the animal was
actually tamed. And because the anchor was never re-taken when an animal stopped being
frightened, resuming jumped the display forward by the full cap in one refresh.

## Decision

Delete `TamingTracker` and its tests. Report the percentage exactly as the record holds it.

## Consequences

- The figure updates in steps of roughly eighteen seconds at vanilla taming durations,
  which is what the underlying data actually supports.
- The panel never shows progress that has not happened, and never jumps.
- 140 lines and twelve tests are gone. The test count fell, and that is the right
  direction: they were testing a mechanism, not a requirement.
- If a mod that shortens taming dramatically ever makes stepping visible, the answer is to
  revisit this with the shorter duration in hand — not to reinstate a predictor that leads
  the authoritative value.

## What this is really about

The tracker was well built, carefully tested, and solved a problem that measurement showed
did not exist. Tests passing says nothing about whether the thing under test should be
there at all.
