# Monetization Design — Diamond Tilt

Goal: recurring monthly revenue from an offline-first baseball game, structured so a server can take over authority later without redesigning the client economy.

## Revenue streams

| Stream | Product | Price (KR W) | Cadence | Notes |
|---|---|---|---|---|
| Subscription | Diamond Pass (daily 30 gems, ad-free, premium battle-pass track while active) | 9,900 / month | Monthly | Auto-renew via store; client stores expiry day-key only |
| Battle pass | Season Pass premium track (per season, ~monthly seasons) | 5,500 / season | Monthly | 30 tiers; premium rewards = gems + cosmetics |
| Consumable IAP | Gem packs 60 / 300(+20 bonus) / 1,000(+100 bonus) | 1,200 / 5,500 / 16,500 | As needed | Store receipts validated before granting |
| Rewarded ads | Optional: +1 mission progress, small coin drops | ad revenue (eCPM) | Per view, capped/day | Never forced; no interstitials mid-match |
| Soft-currency sinks | Uniforms/stadium/bat cosmetics, boosters | Coins/Gems in shop | Ongoing | Sinks keep earned currency meaningful |

## Loop

Play match → XP (win/loss/HR/hits, daily-capped) + coins + mission progress → season tiers → rewards (coins/gems/cosmetics) → spend in shop. Monetization accelerates or decorates; **never pay-to-win**: shop sells cosmetics/boosters only, no stat boosts that alter simulation outcomes.

## Anti-abuse (offline v1)

- Every currency mutation appends to an HMAC-SHA256 hash-chained ledger (`Wallet`); tampering breaks verification on load → quarantine fresh wallet (never crash).
- Purchases are idempotent by order id (store retry storms cannot duplicate grants).
- Receipt validation is a seam (`IReceiptValidator`); v1 ships a dev validator, production wires Unity IAP + server-side receipt check (manual Editor step).
- Clock is injected (`IClock`) so daily/season rollovers are deterministic in tests and cannot be far-future-exploited once server time lands.

## Server-ready contract (post-v1)

All grants/spends already carry reason codes and sequence numbers; moving online = replaying the same operations against a server ledger. Client never self-certifies purchases once networking exists.

## KPI targets

D1 retention ≥ 40%, D7 ≥ 15%, season-pass conversion ≥ 3% of MAU, rewarded-ad engagement ≥ 25% DAU, ARPDAU tracking by stream.
