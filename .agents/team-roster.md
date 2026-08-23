# Team Roster — Delivery & LiveOps

Each role maps to an agent brief under `.agents/agents/`. Load the role file before acting in that role. Any AI tool (Claude Code / Codex / OpenCode) can fill any seat; the human is the Product Owner.

## Delivery pipeline (every change)

| Seat | Brief | Owns |
|---|---|---|
| Analyzer | `agents/analyzer.md` | Finding defects/improvements with evidence, prioritized P0–P2 |
| Designer | `agents/designer.md` | Converting accepted findings into implementable specs + test plans |
| Developer | `agents/developer.md` | Implementation + green suite; only role that edits source |
| Reviewer | `agents/reviewer.md` | Independent verification; blocking count gates every merge |

Loop: repeat until reviewer reports **zero blocking findings**. Ledger: `Docs/pipeline-log.md`.

## Service operation roles

| Seat | Mission | Key artifacts |
|---|---|---|
| Release Manager | Store submissions, staged rollouts, rollbacks, version scheme | `Docs/RUNBOOK.md`, `Docs/BUILD.md` |
| QA Engineer | Device smoke matrix, save-tamper drills, IAP sandbox passes, balance property runs before any config patch | `Docs/RUNBOOK.md` §0, property tests |
| LiveOps Manager | Season cadence, economy KPI review, config-only tuning proposals (via configs, not code) | `Docs/MONETIZATION.md`, `SeasonConfig`/`RewardsConfig`/`MissionsConfig` |
| Security Officer | Schema/PII audit on every SaveData field addition; key-handling decisions | `Docs/SECURITY.md`, `NoPiiAudit` test |

## Working agreements

- Config changes (balance) still go through the full pipeline — they are behavior changes.
- Anything requiring the Unity Editor GUI ends with an explicit Manual Steps list (`rules/agent-editor-boundary.md`).
- Hotfixes branch from release tags and pass identical gates before store re-submission.
