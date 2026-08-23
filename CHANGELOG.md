# Changelog

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
