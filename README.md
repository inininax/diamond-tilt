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

## Play the game (web/three.js — active build)

```sh
open web/index.html            # 그게 전부 — 브라우저에서 바로 플레이
```

- 좌클릭 존 탭 = 투구 · SWING 버튼/Space = 타격 · 모바일 브라우저 터치 지원
- 로직 테스트: `node web/tests/rules.test.js`

## Legacy Unity core

```sh
sh Scripts/run-tests.sh        # 236 EditMode tests via dotnet
```

The original Unity/C# core (rules, physics, economy) is preserved under `Assets/Scripts/` as the reference implementation; `web/` is the active game built on three.js. See `Docs/pipeline-log.md` for the full history.

## Repository map

| Path | Purpose |
|---|---|
| `PROMPT.md` | Game spec, phased plan, production bar, agent-pipeline protocol |
| `AGENTS.md` | Single source of truth for AI coding agents (all tools read it) |
| `.agents/rules/`, `.agents/agents/` | Topic rules + pipeline role definitions |
| `Assets/Scripts/Core/` | Plain-C# domain (rules + economy), Unity-free |
| `Tests/EditMode/` | NUnit suite mirroring production modules |
| `Tests/DotNet/` | Headless test harness (csproj link-compiles Core) |
| `Docs/` | RUNBOOK (ops), BUILD (store builds), SECURITY, MONETIZATION, pipeline-log |
| `Scripts/run-tests.sh` | One-command test gate (CI uses this) |

## Status

Rules engine, ball-flight physics, CPU AI, save integrity, monetization meta, device persistence adapter (`Assets/Scripts/Presentation/SaveStorage.cs`), and a one-command Unity bootstrap are complete; ops runbook + build guide live in `Docs/`. Team roles: `.agents/team-roster.md`. Full history: `Docs/pipeline-log.md`.

Next human steps (Unity Editor required): create project via Unity Hub → run `Diamond Tilt → Bootstrap Project` menu → PlayMode smoke → store builds per `Docs/BUILD.md`.
