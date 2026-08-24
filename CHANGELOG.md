# Changelog

## 0.5.2 - Playable loop closed

- Auto-match demo now feeds the economy (coins, missions, season XP) via GameServices rewards
- Settings persistence (sound toggle) through device save; GameServices difficulty/streak passthrough
- Unity 6 API migrations and boundary test hardening

## 0.5.1 — PlayMode verification

- PlayMode smoke suite (Unity): scene-service composition, full-match-under-budget, device save round-trip
- Bootstrapper registers scenes into build settings; Match scene ships MatchAutoPlayer demo

## 0.5.0 — Unity project live

- Unity 6000.0.82f1 project created from this repo; bootstrapper menu verified end-to-end
- Tests moved under `Assets/Tests/EditMode/**` (mirrored folders); Unity EditMode suite green (229) alongside headless suite (236)
- Input logic core (swipe recognizer, zone grid, touch→intent), string table ko/en, app flow controller
- Consent-gated analytics seam; GameServices reset/difficulty lifecycle; HUD presenter; link.xml guard

## 0.4.0 — Phase 3 logic core + localization

- Touch input logic core: SwipeRecognizer (distance/duration windows), ZoneGrid tap mapping, flick→speed tiers, timing-offset clamping — all pure C#, fully tested
- StringTable (ko/en) + HUD label localization; result labels (win/lose/draw)
- AppFlowController: Boot→Title→Match→Result/Settings state machine with finished-gate navigation and event pumping
- GameServices lifecycle refactor enabling ResetProgress; difficulty exposure
- Follow-up: transition/boundary tests per reviewer notes

## 0.3.0 — Deployment package

- Ops: RUNBOOK (launch gates, rollout/rollback, compliance, LiveOps calendar, incident response), BUILD guide (iOS/Android)
- Unity bootstrap: one-menu Editor script (asmdefs, player settings, scenes); Presentation GameRunner + device save persistence (HMAC envelope, atomic write, quarantine)
- Team roster for delivery + LiveOps roles
- Consent-gated analytics seam (COPPA-safe default-deny); GameServices.ResetProgress / CurrentDifficulty; HUD view-model presenter; link.xml IL2CPP guard

## 0.2.0 — Monetization meta-layer

- Wallet with HMAC-chained ledger, idempotent shop orders, season pass (monthly rollover, premium track), daily missions, capped rewarded ads, IAP receipt-validation seam, stacking subscription entitlements
- Save schema v2 + v1 migration; clamp-before-use everywhere; PII-free schema audit

## 0.1.0 — Playable core

- Baseball rules engine (at-bats, base-running, innings, walk-off rule), analytic ball flight with drag + wall/foul adjudication, CPU pitcher/batter AI with difficulty tiers, auto-match simulator
- Determinism pinned by property tests; seeded RNG single entry point
- Headless test harness: full suite runs without Unity (`Scripts/run-tests.sh`)
