# Security Notes (v1 offline)

## Save data integrity
- Save envelope = `{ payload, tag }`; `tag` is HMAC-SHA256(payload, key), hex-encoded (`Assets/Scripts/Core/SaveIntegrity.cs`).
- Verification uses `CryptographicOperations.FixedTimeEquals` (no timing side channel).
- On any load failure (corrupt JSON, tampered payload, wrong key, unknown schema version) the loader returns `false` and callers keep a fresh state — quarantine semantics, never a crash (`Tests/DotNet/Adapters/SaveJsonAdapter.cs`).
- Every deserialized numeric field is range-clamped before use (`SaveClamp.Clamp`) — hand-edited saves cannot produce illegal game states.

## Key handling
- v1 key derivation: device-local random seed (`SaveIntegrity.DeriveKey`). Acceptable for an offline single-player game where the only goal is corruption/tamper resistance.
- Known limitation (documented, intentional): the key lives on the same device as the save, so a determined user can re-sign their own edits. This is fine for v1; when online features land, move verification server-side and store keys in platform keystores (iOS Keychain / Android Keystore). Do NOT ship any secret in the client binary.

## Privacy
- The save schema contains no PII and no identifiers — enforced by a whitelist audit test (`NoPiiAudit_SerializedKeysWhitelisted`).
- No analytics/ad SDKs in v1. Any future telemetry requires a consent gate (COPPA / GDPR-K aware audience).
- Debug logging of player data is prohibited; release builds strip debug logs.

## Supply chain
- Runtime dependencies in Core: none (plain C#, netstandard2.1 surface). Test harness adds NUnit + System.Text.Json — dev-only, never shipped to players.
- Third-party assets/packages must be recorded in `THIRD-PARTY-NOTICES.md` with license before import.

## Networking (post-v1 contract)
- Server-authoritative results, TLS + certificate pinning, rate limiting. Client never decides wins/rewards.
