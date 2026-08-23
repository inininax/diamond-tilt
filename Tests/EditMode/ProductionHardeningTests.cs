using System;
using DiamondTilt.Core;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class ProductionHardeningTests
    {
        private static readonly DateTime BaseUtc = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
        private static readonly byte[] Key = SaveIntegrity.DeriveKey(808u);

        private static MatchEngine EngineAtBottomFinal(int awayRuns, int homeRuns, bool first = false, bool second = false, bool third = false)
        {
            var engine = new MatchEngine(new StubContactModel(PlayOutcome.Grounder));
            engine.State.Inning = MatchState.Innings;
            engine.State.IsTop = false;
            engine.State.AwayRuns = awayRuns;
            engine.State.HomeRuns = homeRuns;
            if (first) engine.State.FirstBase = true;
            if (second) engine.State.SecondBase = true;
            if (third) engine.State.ThirdBase = true;
            return engine;
        }

        [Test]
        public void Walkoff_HomeTakesLead_BottomFinal_FinishesImmediately()
        {
            var engine = EngineAtBottomFinal(awayRuns: 1, homeRuns: 1, third: true);
            MatchTestHarness.ContactPitch(engine);

            Assert.That(engine.State.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(engine.State.Result, Is.EqualTo(Winner.Home));

            int eventCount = engine.DrainEvents().Count;
            MatchTestHarness.ContactPitch(engine);
            Assert.That(engine.DrainEvents().Count, Is.EqualTo(0));
            Assert.That(eventCount, Is.GreaterThan(0));
        }

        [Test]
        public void Walkoff_HomerunBottomFinal_WinsInstantly()
        {
            var engine = new MatchEngine(new FixedResolver(PlayOutcome.DeepFly, new LaunchParams(52, 31, 0)));
            engine.State.Inning = MatchState.Innings;
            engine.State.IsTop = false;
            engine.State.AwayRuns = 2;
            engine.State.HomeRuns = 2;

            MatchTestHarness.ContactPitch(engine);

            Assert.That(engine.State.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(engine.State.Result, Is.EqualTo(Winner.Home));
            Assert.That(engine.State.HomeRuns, Is.EqualTo(3));
        }

        [Test]
        public void Walkoff_NotFinalInning_Continues()
        {
            var engine = new MatchEngine(new StubContactModel(PlayOutcome.Grounder));
            engine.State.Inning = 1;
            engine.State.IsTop = false;
            engine.State.AwayRuns = 5;
            engine.State.HomeRuns = 0;
            engine.State.ThirdBase = true;

            MatchTestHarness.ContactPitch(engine);

            Assert.That(engine.State.Phase, Is.EqualTo(MatchPhase.InProgress));
        }

        [Test]
        public void Walkoff_HomeStillTrailingAfterRun_Continues()
        {
            var engine = new MatchEngine(new StubContactModel(PlayOutcome.Grounder));
            engine.State.Inning = MatchState.Innings;
            engine.State.IsTop = false;
            engine.State.AwayRuns = 5;
            engine.State.HomeRuns = 0;
            engine.State.ThirdBase = true;

            MatchTestHarness.ContactPitch(engine);

            Assert.That(engine.State.HomeRuns, Is.EqualTo(1));
            Assert.That(engine.State.Phase, Is.EqualTo(MatchPhase.InProgress));
        }

        private sealed class FixedResolver : IContactResolver
        {
            private readonly PlayOutcome _outcome;
            private readonly LaunchParams _flight;

            public FixedResolver(PlayOutcome outcome, LaunchParams flight)
            {
                _outcome = outcome;
                _flight = flight;
            }

            public ContactResolution Evaluate(PitchCall pitch, SwingDecision swing, int absOffsetTicks, int perfectBandTicks)
                => new ContactResolution(_outcome, _flight);
        }

        [Test]
        public void Season_NegativeCounters_ClampedToZero()
        {
            var clock = new FixedClock(BaseUtc);
            var season = new SeasonPassSystem(null, new Wallet(Key), clock);

            int xp = season.RecordMatch(true, hits: -5, homeruns: -9);

            Assert.That(xp, Is.EqualTo(SeasonRules.WinXp));
            Assert.That(season.State.Xp, Is.EqualTo(SeasonRules.WinXp));
        }

        [Test]
        public void Season_FreshState_SeasonsCompletedZero_AndIncrementsOncePerRollover()
        {
            var clock = new FixedClock(BaseUtc);
            var season = new SeasonPassSystem(null, new Wallet(Key), clock);

            Assert.That(season.State.SeasonsCompleted, Is.EqualTo(0));

            clock.AdvanceDays(40);
            season.EnsureSeason();
            Assert.That(season.State.SeasonsCompleted, Is.EqualTo(1));
        }

        [Test]
        public void Clamp_SubscriptionFarFuture_NormalizedToEmpty()
        {
            var d = new SaveData { Subscription = new SubscriptionState { ExpiryDayKey = "9999-12-31" } };

            SaveClamp.Clamp(d);

            Assert.That(d.Subscription.ExpiryDayKey, Is.Empty);
        }

        [Test]
        public void Clamp_SubscriptionValidNearDate_Preserved_MalformedEmptied()
        {
            var d = new SaveData
            {
                Subscription = new SubscriptionState { ExpiryDayKey = "not-a-date" },
            };
            SaveClamp.Clamp(d);
            Assert.That(d.Subscription.ExpiryDayKey, Is.Empty);

            d.Subscription.ExpiryDayKey = TimeKeys.DayKey(BaseUtc.AddDays(30));
            SaveClamp.Clamp(d);
            Assert.That(d.Subscription.ExpiryDayKey, Is.EqualTo(TimeKeys.DayKey(BaseUtc.AddDays(30))));
        }

        [Test]
        public void Restore_InProgress_OutsClampedToTwo()
        {
            var state = new MatchState();
            state.Restore(new MatchSnapshot { Phase = (int)MatchPhase.InProgress, Outs = 3 });

            Assert.That(state.Outs, Is.EqualTo(2));
        }

        [Test]
        public void Reconcile_LedgerTailWins_HealsScalars()
        {
            var clock = new FixedClock(BaseUtc);
            var wallet = new Wallet(Key);
            wallet.Grant(CurrencyType.Coins, 250, "r", clock);
            wallet.Grant(CurrencyType.Gems, 15, "r", clock);

            var data = new SaveData
            {
                WalletCoins = 999999,
                WalletGems = 999999,
                Ledger = new System.Collections.Generic.List<LedgerEntry>(wallet.Entries),
            };

            Assert.That(WalletReconciliation.Apply(data, Key), Is.EqualTo(ReconcileStatus.Reconciled));
            Assert.That(data.WalletCoins, Is.EqualTo(250));
            Assert.That(data.WalletGems, Is.EqualTo(15));
        }

        [Test]
        public void Reconcile_EmptyLedger_ScalarsStand()
        {
            var data = new SaveData { WalletCoins = 42, WalletGems = 7 };

            Assert.That(WalletReconciliation.Apply(data, Key), Is.EqualTo(ReconcileStatus.NoLedger));
            Assert.That(data.WalletCoins, Is.EqualTo(42));
        }

        [Test]
        public void Wallet_GrantOverCap_ClampsAmount_DeltaEqualsAmount()
        {
            var clock = new FixedClock(BaseUtc);
            var w = new Wallet(Key, coins: WalletTestHelpers.MaxBalance - 5);

            w.Grant(CurrencyType.Coins, 1000, "over", clock);

            var last = w.Entries[w.Entries.Count - 1];
            Assert.That(last.Amount, Is.EqualTo(5));
            Assert.That(w.Coins, Is.EqualTo(WalletTestHelpers.MaxBalance));
        }

        [Test]
        public void Wallet_GrantAtCap_NoEntry_NoStateChange()
        {
            var clock = new FixedClock(BaseUtc);
            var w = new Wallet(Key, coins: WalletTestHelpers.MaxBalance);

            w.Grant(CurrencyType.Coins, 500, "at-cap", clock);

            Assert.That(w.Entries.Count, Is.EqualTo(0));
            Assert.That(w.Coins, Is.EqualTo(WalletTestHelpers.MaxBalance));
        }

        [Test]
        public void Entitlements_ExclusiveExpiry_CoversExactlyThirtyDistinctDays()
        {
            var clock = new FixedClock(BaseUtc);
            var ent = new EntitlementService(null, clock);
            ent.ActivateOrExtend(30);

            for (int day = 0; day <= 29; day++)
            {
                clock.UtcNow = BaseUtc.AddDays(day);
                Assert.That(ent.HasActiveSubscription(), Is.True, $"day {day}");
            }
            for (int day = 30; day <= 32; day++)
            {
                clock.UtcNow = BaseUtc.AddDays(day);
                Assert.That(ent.HasActiveSubscription(), Is.False, $"day {day}");
            }
        }

        [Test]
        public void Entitlements_RenewOnFinalCoveredDay_GaplessExtension()
        {
            var clock = new FixedClock(BaseUtc);
            var ent = new EntitlementService(null, clock);
            ent.ActivateOrExtend(30);

            clock.UtcNow = BaseUtc.AddDays(29);
            ent.ActivateOrExtend(30);

            Assert.That(ent.State.ExpiryDayKey, Is.EqualTo(TimeKeys.DayKey(BaseUtc.AddDays(60))));

            for (int day = 30; day <= 59; day++)
            {
                clock.UtcNow = BaseUtc.AddDays(day);
                Assert.That(ent.HasActiveSubscription(), Is.True, $"day {day}");
            }
            clock.UtcNow = BaseUtc.AddDays(60);
            Assert.That(ent.HasActiveSubscription(), Is.False);
        }
    }

    internal static class WalletTestHelpers
    {
        public const long MaxBalance = 1_000_000_000_000;
    }
}
