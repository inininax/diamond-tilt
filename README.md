# Diamond Tilt

2.5D baseball game for mobile (iOS + Android), built with Unity (C#).
The gameplay/economy core is plain C# with zero `UnityEngine` dependencies — the full simulation and meta systems run and test headlessly via .NET.

## Architecture

```
┌─ Presentation (Unity, Phase 3+) ─────────────────────────┐
│ MonoBehaviour adapters: input, sprites, HUD, audio        │
│ consumes events only — never mutates game state           │
└──────────────┬───────────────────────────────────────────┘
               │ intents in / drained events out
┌──────────────▼───────────────────────────────────────────┐
│ Application  · GameServices (composition root)            │
│              · AutoMatch simulator                        │
├──────────────────────────────────────────────────────────┤
│ Domain       · Rules: MatchEngine, BaseRunnerEngine,      │
│                BallFlight, contact resolvers              │
│              · Economy: Wallet (HMAC ledger), Shop,       │
│                SeasonPass, DailyMissions, IAP/entitlements│
├──────────────────────────────────────────────────────────┤
│ Ports        · IClock, IRngService, IReceiptValidator,    │
│                IContactResolver (all injected)            │
└───────────────────────────────────────────────────────────┘
```

- **Determinism**: fixed-tick sim, seeded mulberry32 RNG behind `IRngService`; same seed → identical event stream (pinned by property tests).
- **Save security**: HMAC-SHA256 envelope, hash-chained currency ledger, clamp-before-use, schema-versioned migration. See `Docs/SECURITY.md`.
- **Monetization**: subscription + season pass + IAP gems + capped rewarded ads; never pay-to-win. See `Docs/MONETIZATION.md`.

## Quickstart (no Unity required)

```sh
sh Scripts/run-tests.sh        # 194+ EditMode tests via dotnet
```

Requires the .NET SDK. Unity-side work (scenes, prefabs, device builds, store SDKs) is tracked in `AGENTS.md` → "In-progress work".

## Repository map

| Path | Purpose |
|---|---|
| `PROMPT.md` | Game spec, phased plan, production bar, agent-pipeline protocol |
| `AGENTS.md` | Single source of truth for AI coding agents (all tools read it) |
| `.agents/rules/`, `.agents/agents/` | Topic rules + pipeline role definitions |
| `Assets/Scripts/Core/` | Plain-C# domain (rules + economy), Unity-free |
| `Tests/EditMode/` | NUnit suite mirroring production modules |
| `Tests/DotNet/` | Headless test harness (csproj link-compiles Core) |
| `Docs/` | SECURITY, MONETIZATION, pipeline-log |
| `Scripts/run-tests.sh` | One-command test gate (CI uses this) |

## Status

Rules engine, ball-flight physics, CPU AI, save integrity, and monetization meta are complete and reviewed headlessly (pipeline log: `Docs/pipeline-log.md`). Next: create the Unity project, wire asmdefs + touch input (Phase 3), then store SDKs behind existing seams.
