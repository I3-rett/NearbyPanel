# ADR 0002 — Target net48, ship no publicizer

**Status:** accepted

## Context

Valheim 1.0.16 runs on Unity 6000.0.75, whose Mono exposes the `NET_Unity_4_8` API
profile: .NET Framework 4.8 plus some .NET Standard 2.1 surface. Both current reference
projects agree — `Valheim-Modding/JotunnModStub` targets `net48`, `xtavim/BetterMap`
targets `v4.8`.

Valheim's types are public but many members are not. The usual answer is a publicized
copy of the game assembly, via `Krafs.Publicizer` or a NuGet package of pre-publicized
reference assemblies.

Checking what this mod actually needs: `Character.GetCharactersInRange`,
`GetAllCharacters`, `GetHoverName`, `GetLevel`, `GetCenterPoint`, `GetFaction`,
`GetHealthPercentage`, `IsPlayer`, `IsBoss`, `IsDead`; `Tameable.IsTamed`, `IsHungry`,
`GetStatusString`, `GetName`, `m_tamingTime`; `ZDO.GetFloat`, `GetPosition`;
`ZDOVars.s_tameTimeLeft`; `BaseAI.IsAlerted`, `HaveTarget`, `GetTimeSinceSpawned`. Every
one is public. BetterMap points its references at publicized assemblies and then uses 254
public members and zero non-public ones — it publicizes out of habit.

## Decision

Target `net48`. Reference the game assemblies straight from the Steam install with
`<Private>False</Private>`. No publicizer, no reflection.

Set `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` anyway.

## Consequences

- The project builds with `dotnet build` and no extra tooling, and nothing of Iron Gate's
  is generated inside the repository.
- It does not build on a machine without Valheim installed. Accepted: it is a Valheim mod.
  The API guard tests are written to pass rather than fail in that situation so the rest
  of the suite still runs.
- `AllowUnsafeBlocks` makes the compiler emit the `SkipVerification` security attribute.
  Mono's JIT enforces access checks without it: across the seventeen plugin DLLs in the
  reference profile, all six that touch a non-public game member carry that attribute and
  none omit it. Setting it now means the first private-member access will not fail with a
  `MemberAccessException` that looks like a mystery.
- If a private member ever becomes necessary, prefer Harmony's injected `ref T ___m_field`
  parameter, then `AccessTools.FieldRefAccess` cached in a static, then
  `Krafs.Publicizer` — in that order.
