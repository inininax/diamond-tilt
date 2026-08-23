# Operations Runbook — Diamond Tilt

Owner: Release Manager (see `.agents/team-roster.md`). Update this file whenever the launch surface changes.

## 0. Readiness gates (all must be ✅ before store submission)

- [ ] Unity project created and `Assets/Scripts/Editor/DiamondTiltBootstrapper.cs` menu run once (creates asmdefs, scenes, wiring)
- [ ] `sh Scripts/run-tests.sh` green on CI (`Actions → tests`) for the release commit
- [ ] PlayMode smoke test passes on device: full 3-inning match, background/foreground mid-at-bat, save→kill→relaunch restores state
- [ ] IAP sandbox purchases: all gem packs + Diamond Pass + Season Premium, including interrupted-purchase retry (order idempotency)
- [ ] Rewarded-ad flow grants capped bonuses only
- [ ] Save tamper drill: hand-edited save quarantines to fresh state without crash
- [ ] Privacy: policy URL live, App Store privacy labels + Play Data Safety form filled (schema is PII-free — `NoPiiAudit` test)
- [ ] Version/build numbers bumped per scheme in `Docs/BUILD.md`
- [ ] Crash reporting decision executed: pick ONE of Unity Cloud Diagnostics or Firebase Crashlytics, add SDK behind `Assets/Scripts/Presentation/` adapter seam, symbol upload wired in CI (see crash guidance in `Docs/BUILD.md`)
- [ ] Data-deletion path verified: settings → "Reset Save" wipes local file; no server data exists in v1 (no accounts)

## 1. Build & release flow

1. Cut release branch `release/x.y.z`; bump version in `ProjectSettings/ProjectSettings.asset`.
2. CI must be green; tag `v x.y.z`.
3. Device builds per `Docs/BUILD.md` (IL2CPP, AAB for Play, IPA for TestFlight).
4. Internal track / TestFlight validation ≥ 24h with the QA checklist below.
5. Staged rollout: Play 10% → 50% → 100%; iOS phased release 7 days.
6. Rollback = halt staged rollout + previous build re-submitted (store builds are immutable; keep every tagged artifact).

## 2. Store assets checklist

- Icons (1024 iOS / 512 Android), screenshots per required device classes, feature graphic
- Description (ko/en), keyword set, age rating questionnaires (both stores)
- Privacy policy URL + terms URL

## 3. Compliance

- Audience: sports game playable by minors ⇒ no behavioral ads without consent gate; rewarded ads only after age-gate answer where required (COPPA/GDPR-K).
- No PII collected in v1 (enforced by test). Any telemetry addition requires: consent UI, schema review, this doc update.

## 4. LiveOps calendar

| Cadence | Operation | Owner |
|---|---|---|
| Monthly | Season rollover sanity check on build 1st of month (XP reset, premium persistence via subscription) | LiveOps |
| Weekly | Economy KPI review: ARPDAU by stream, pass conversion, walk-rate/avg-runs from device telemetries (when enabled) | LiveOps |
| Per patch | Re-run balance properties (`AutoMatch_Balance_TotalRunsInSaneBand`, homerun occurrence) after any config change | QA |
| As needed | Config-only tuning via `SeasonConfig`/`RewardsConfig`/`MissionsConfig` — no code change; still requires full suite green | Designer |

## 5. Incident response

| Symptom | First response |
|---|---|
| Crash spike on new build | Halt rollout; pull symbolicated stack; check `MatchEngine` hot paths for regressions vs last tag |
| Economy anomaly (impossible balances) | Ledger chain verification flags at load — pull affected-save diagnostics; if exploit, ship clamp/config hotfix |
| IAP failures spike | Check store status pages first; then receipt-validator adapter logs; never grant manually client-side |
| Save corruption reports | Confirm quarantine path (fresh save, not crash); investigate schema/migration diff |
| Data-deletion request | v1 has no accounts/PII: instruct uninstall (local save wiped). If telemetry added later, fulfilment flow becomes mandatory within store SLAs |

## Remote config story

Balance lives in compile-time config objects (`SeasonConfig`, `RewardsConfig`, `MissionsConfig`) — tuning ships as PATCH releases until a remote-config service is adopted. Adoption order when needed: fetch JSON → validate with `GameConfigValidator` → swap instances at boot; never trust client-fetched values for entitlements.

Escalation: blocking production issue ⇒ rollback per §1.6, then fix via standard pipeline (analyzer→designer→developer→reviewer).

## 6. Post-launch development loop

Every change follows `.agents/agents/*.md` pipeline; ledger entries append to `Docs/pipeline-log.md`. Hotfixes branch from the release tag and re-run the same gates.
