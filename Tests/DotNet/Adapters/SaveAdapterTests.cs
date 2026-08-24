using System;
using System.Collections.Generic;
using System.Text.Json;
using DiamondTilt.Core;
using DiamondTilt.Core.Economy;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class SaveAdapterTests
    {
        private static readonly byte[] Key = SaveIntegrity.DeriveKey(4041u);
        private static readonly DateTime BaseUtc = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);

        private static MatchEngine MidGameEngine()
        {
            var engine = new MatchEngine(new TimingContactModel());
            AutoMatch.PlaySelfContained(engine, Difficulty.Normal, 555u);
            return engine.State.Phase == MatchPhase.Finished ? new MatchEngine(new TimingContactModel()) : engine;
        }

        [Test]
        public void Adapter_TryLoad_HappyPath_RoundTrips()
        {
            var engine = MidGameEngine();
            var data = new SaveData { Match = engine.State.ToSnapshot(), Wins = 3, Losses = 1 };
            string json = SaveJsonAdapter.SerializeEnvelope(data, Key);

            bool ok = SaveJsonAdapter.TryLoad(json, Key, out var loaded);

            Assert.That(ok, Is.True);
            Assert.That(loaded.Wins, Is.EqualTo(3));
            Assert.That(loaded.Match.HomeRuns, Is.EqualTo(data.Match.HomeRuns));
        }
        [Test]
        public void Adapter_CorruptJson_ReturnsFalse_NoThrow()
        {
            Assert.That(SaveJsonAdapter.TryLoad("{not json", Key, out _), Is.False);
            Assert.That(SaveJsonAdapter.TryLoad("", Key, out _), Is.False);
            Assert.That(SaveJsonAdapter.TryLoad(null, Key, out _), Is.False);
        }
        [Test]
        public void Adapter_TamperedEnvelope_Rejected_EndToEnd()
        {
            var data = new SaveData { Match = new MatchSnapshot { HomeRuns = 5 } };
            string payload = JsonSerializer.Serialize(data, SaveJsonAdapter.Options);
            var envelope = new SaveEnvelope { Payload = payload.Replace("5", "500"), Tag = SaveIntegrity.Tag(payload, Key) };
            string json = JsonSerializer.Serialize(envelope);

            Assert.That(SaveJsonAdapter.TryLoad(json, Key, out _), Is.False);
        }
        [Test]
        public void Adapter_QuarantineSemantics_LoadFailureLeavesFreshStateUsable()
        {
            var engine = new MatchEngine(new TimingContactModel());
            bool ok = SaveJsonAdapter.TryLoad("garbage", Key, out _);

            Assert.That(ok, Is.False);
            MatchTestHarness.TakeStrike(engine);
            Assert.That(engine.State.Strikes, Is.EqualTo(1));
        }
        [Test]
        public void NoPiiAudit_SerializedKeysWhitelisted()
        {
            var data = new SaveData { Match = new MatchSnapshot(), Wins = 1, Losses = 2, DifficultyTier = 1 };
            var doc = JsonDocument.Parse(JsonSerializer.Serialize(data, SaveJsonAdapter.Options));

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                Assert.That(prop.Name, Is.AnyOf("SchemaVersion", "Match", "Wins", "Losses", "DifficultyTier",
                        "CurrentStreak", "BestStreak",
                        "WalletCoins", "WalletGems", "Ledger", "SeasonPass", "Missions", "Subscription",
                        "PurchaseOrders", "IapOrders", "OwnedShopItems", "SoundEnabled"),
                    "unexpected top-level key — possible PII leak");
            }
            foreach (var prop in doc.RootElement.GetProperty("Match").EnumerateObject())
            {
                Assert.That(prop.Name, Is.AnyOf(
                    "Inning", "IsTop", "Balls", "Strikes", "Outs",
                    "FirstBase", "SecondBase", "ThirdBase", "AwayRuns", "HomeRuns", "Phase", "Result"),
                    $"unexpected match key '{prop.Name}' — possible PII leak");
            }
        }
        [Test]
        public void Adapter_Load_BalancesEqualLedgerTail_RoundTrip()
        {
            var clock = new FixedClock(new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc));
            var wallet = new Wallet(Key);
            wallet.Grant(CurrencyType.Coins, 300, "r", clock);
            wallet.Spend(CurrencyType.Coins, 50, "r", clock);
            wallet.Grant(CurrencyType.Gems, 12, "r", clock);

            var data = new SaveData
            {
                WalletCoins = wallet.Coins,
                WalletGems = wallet.Gems,
                Ledger = new System.Collections.Generic.List<LedgerEntry>(wallet.Entries),
            };
            string json = SaveJsonAdapter.SerializeEnvelope(data, Key);

            bool ok = SaveJsonAdapter.TryLoad(json, Key, out var loaded);

            Assert.That(ok, Is.True);
            Assert.That(loaded.WalletCoins, Is.EqualTo(250));
            Assert.That(loaded.WalletGems, Is.EqualTo(12));
        }
        [Test]
        public void Adapter_CorruptLedgerChain_FailsClosed()
        {
            var clock = new FixedClock(new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc));
            var wallet = new Wallet(Key);
            wallet.Grant(CurrencyType.Gems, 40, "r", clock);

            var data = new SaveData
            {
                WalletGems = 40,
                Ledger = new System.Collections.Generic.List<LedgerEntry>(wallet.Entries),
            };
            data.Ledger[0].Amount = 40000;
            string json = SaveJsonAdapter.SerializeEnvelope(data, Key);

            Assert.That(SaveJsonAdapter.TryLoad(json, Key, out _), Is.False);
        }
    }
}
