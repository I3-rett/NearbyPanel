# ADR 0001 — No Jotunn, no other runtime dependency

**Status:** accepted

## Context

Jotunn 2.30.2 is the standard Valheim modding framework and is already installed in the
target profile. It offers a Valheim-styled draggable panel (`CreateWoodpanel`), a ready
scroll view whose content already carries a `VerticalLayoutGroup` and a
`ContentSizeFitter`, the game fonts and sprites, `GUIManager.BlockInput`, chat-safe and
gamepad-aware hotkeys, and localization helpers.

Two mods already installed prove it is not required: Valheim Foresight ships both
in-world bars and a full toggleable window with no Jotunn at all, and HealthBarPlus
extends the existing HUD with two Harmony patches and nothing else.

## Decision

Depend on BepInEx only. Draw the panel with legacy IMGUI in `OnGUI`.

## Consequences

- `manifest.json` carries a single dependency, `denikson-BepInExPack_Valheim`. Every
  extra dependency is a version to keep in step and a support question to answer.
- We give up the native-looking wood panel and `BlockInput`. Acceptable: a read-only
  list that takes no clicks needs no input blocking, and IMGUI is enough for a table.
- If the panel ever becomes interactive, revisit this. `GUIManager.BlockInput(true/false)`
  replaces what would otherwise be a large amount of hand-written input suppression —
  shudnal's ConfigurationManager spends thirteen Harmony patch classes on exactly that.
- IMGUI redraws on every `OnGUI` event and allocates. At one small window refreshed a few
  times a second this does not matter.
