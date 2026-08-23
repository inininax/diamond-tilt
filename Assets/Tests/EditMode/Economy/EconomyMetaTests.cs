using System;
using DiamondTilt.Core;
using DiamondTilt.Core.Economy;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class EconomyMetaTests
    {
        private static readonly DateTime BaseUtc = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
        private static readonly byte[] Key = SaveIntegrity.DeriveKey(321u);

        private static (FixedClock Clock, Wallet Wallet) Setup(long coins = 0, long gems = 0)
            => (new FixedClock(BaseUtc), new Wallet(Key, coins, gems));

        [Test]
        public void Season_EnsureSeason_CreatesCurrentId()
        {
            var (clock, wallet) = Setup();
            var season = new SeasonPassSystem(null, wallet, clock);

            Assert.That(season.State.SeasonId, Is.EqualTo("2026-08"));
        }

        [Test]
        public void Season_RecordMatch_XpMath()
        {
            var (clock, wallet) = Setup();
            var season = new SeasonPassSystem(null, wallet, clock);

            int xp = season.RecordMatch(won: true, hits: 3, homeruns: 1);

            Assert.That(xp, Is.EqualTo(SeasonRules.WinXp + 3 * SeasonRules.PerHitXp + SeasonRules.PerHrXp));
        }

        [Test]
        public void Season_DailyCap_Enforced_AndResetsNextDay()
        {
            var (clock, wallet) = Setup();
            var season = new SeasonPassSystem(null, wallet, clock);

            for (int i = 0; i < 10; i++) season.RecordMatch(true, 5, 5);
            int cappedTotal = season.State.Xp;
            Assert.That(cappedTotal, Is.LessThanOrEqualTo(SeasonRules.MaxDailyXp));

            clock.AdvanceDays(1);
            season.RecordMatch(false, 0, 0);
            Assert.That(season.State.Xp, Is.GreaterThan(cappedTotal));
        }

        [Test]
        public void Season_TierUnlock_Boundary()
        {
            var (clock, wallet) = Setup();
            var season = new SeasonPassSystem(null, wallet, clock);

            Assert.That(season.IsTierUnlocked(1), Is.False);

            season.RecordMatch(true, 20, 0);
            Assert.That(season.IsTierUnlocked(1), Is.True);
        }

        [Test]
        public void Season_ClaimFree_GrantsCoinsOnce()
        {
            var (clock, wallet) = Setup();
            var season = new SeasonPassSystem(null, wallet, clock);
            season.RecordMatch(true, 20, 0);

            Assert.That(season.ClaimReward(1), Is.EqualTo(PurchaseResult.Success));
            Assert.That(wallet.Coins, Is.EqualTo(SeasonRules.FreeCoinReward(1)));
            Assert.That(season.ClaimReward(1), Is.EqualTo(PurchaseResult.DuplicateOrder));
            Assert.That(wallet.Coins, Is.EqualTo(SeasonRules.FreeCoinReward(1)));
        }

        [Test]
        public void Season_PremiumTier_GatedByOwnership()
        {
            var (clock, wallet) = Setup();
            var season = new SeasonPassSystem(null, wallet, clock);
            season.SetPremiumOwned(false);
            season.RecordMatch(true, 50, 50);
            clock.AdvanceDays(1);
            season.RecordMatch(true, 50, 50);

            Assert.That(season.ClaimReward(5), Is.EqualTo(PurchaseResult.InsufficientFunds));

            season.SetPremiumOwned(true);
            long gemsBefore = wallet.Gems;
            Assert.That(season.ClaimReward(5), Is.EqualTo(PurchaseResult.Success));
            Assert.That(wallet.Gems, Is.GreaterThan(gemsBefore));
        }

        [Test]
        public void Season_Rollover_ResetsProgress_PremiumPersistsOnlyWithSubscription()
        {
            var (clock, wallet) = Setup();
            bool subscribed = true;
            var season = new SeasonPassSystem(null, wallet, clock, () => subscribed);
            season.SetPremiumOwned(true);
            season.RecordMatch(true, 20, 0);
            season.ClaimReward(1);

            clock.AdvanceDays(40);
            season.EnsureSeason();

            Assert.That(season.State.SeasonId, Is.EqualTo("2026-10"));
            Assert.That(season.State.Xp, Is.EqualTo(0));
            Assert.That(season.State.PremiumOwned, Is.True);

            subscribed = false;
            clock.AdvanceDays(32);
            season.EnsureSeason();

            Assert.That(season.State.PremiumOwned, Is.False);
        }

        [Test]
        public void Missions_ProgressAccumulates_AndReadyListCorrect()
        {
            var (clock, wallet) = Setup();
            var missions = new DailyMissionSystem(null, wallet, clock);

            missions.RecordMatch(true, hits: 3, homeruns: 0);
            CollectionAssert.AreEquivalent(new[] { "win_1" }, missions.ReadyMissionIds());

            missions.RecordMatch(true, hits: 2, homeruns: 1);
            CollectionAssert.AreEquivalent(
                new[] { "play_2", "hits_5", "hr_1", "win_1" }, missions.ReadyMissionIds());
        }

        [Test]
        public void Missions_ClaimGrantsExactGems_OncePerDay()
        {
            var (clock, wallet) = Setup();
            var missions = new DailyMissionSystem(null, wallet, clock);
            missions.RecordMatch(true, 5, 1);

            Assert.That(missions.Claim("hr_1"), Is.EqualTo(PurchaseResult.Success));
            Assert.That(wallet.Gems, Is.EqualTo(5));
            Assert.That(missions.Claim("hr_1"), Is.EqualTo(PurchaseResult.DuplicateOrder));
            Assert.That(wallet.Gems, Is.EqualTo(5));
        }

        [Test]
        public void Missions_NotReadyClaim_Rejected()
        {
            var (clock, wallet) = Setup();
            var missions = new DailyMissionSystem(null, wallet, clock);

            Assert.That(missions.Claim("win_1"), Is.EqualTo(PurchaseResult.InvalidInput));
        }

        [Test]
        public void Missions_NextDayResetsEverything()
        {
            var (clock, wallet) = Setup(gems: 0);
            var missions = new DailyMissionSystem(null, wallet, clock);
            missions.RecordMatch(true, 9, 9);
            missions.Claim("win_1");

            clock.AdvanceDays(1);
            missions.EnsureDay();

            Assert.That(missions.ReadyMissionIds(), Is.Empty);
            Assert.That(missions.Claim("win_1"), Is.EqualTo(PurchaseResult.InvalidInput));
        }

        [Test]
        public void AdBonus_GrantsCoins_UntilDailyCap()
        {
            var (clock, wallet) = Setup();
            var missions = new DailyMissionSystem(null, wallet, clock);

            for (int i = 0; i < MissionRules.MaxAdBonusesPerDay; i++)
            {
                Assert.That(missions.ClaimRewardedAdBonus(), Is.EqualTo(PurchaseResult.Success));
            }
            long afterCap = wallet.Coins;
            Assert.That(missions.ClaimRewardedAdBonus(), Is.EqualTo(PurchaseResult.InvalidInput));
            Assert.That(wallet.Coins, Is.EqualTo(afterCap));
        }

        [Test]
        public void Entitlements_ActivateExtendsFromToday()
        {
            var (clock, _) = Setup();
            var ent = new EntitlementService(null, clock);

            ent.ActivateOrExtend(IapCatalog.SubscriptionDays);
            Assert.That(ent.HasActiveSubscription(), Is.True);
            Assert.That(ent.State.ExpiryDayKey, Is.EqualTo(TimeKeys.DayKey(BaseUtc.AddDays(30))));

            clock.AdvanceDays(10);
            ent.ActivateOrExtend(IapCatalog.SubscriptionDays);
            Assert.That(ent.State.ExpiryDayKey, Is.EqualTo(TimeKeys.DayKey(BaseUtc.AddDays(60))));
        }

        [Test]
        public void Entitlements_ExpiredSubscription_InactiveUntilRenewed()
        {
            var (clock, _) = Setup();
            var ent = new EntitlementService(null, clock);
            ent.ActivateOrExtend(30);

            clock.AdvanceDays(31);
            Assert.That(ent.HasActiveSubscription(), Is.False);

            ent.ActivateOrExtend(IapCatalog.SubscriptionDays);
            Assert.That(ent.HasActiveSubscription(), Is.True);
            Assert.That(ent.State.ExpiryDayKey, Is.EqualTo(TimeKeys.DayKey(BaseUtc.AddDays(61))));
        }

        [Test]
        public void FakeValidator_RejectsTamperedAndEmpty()
        {
            var v = new FakeReceiptValidator();

            Assert.That(v.Validate("iap_gems_60", "valid-receipt"), Is.True);
            Assert.That(v.Validate("iap_gems_60", "TAMPERED"), Is.False);
            Assert.That(v.Validate("iap_gems_60", ""), Is.False);
            Assert.That(v.Validate("", "receipt"), Is.False);
        }

        [Test]
        public void Iap_GemPack_GrantsExactGems_IdempotentByOrderId()
        {
            var (clock, wallet) = Setup();
            var ent = new EntitlementService(null, clock);
            var season = new SeasonPassSystem(null, wallet, clock);
            var iap = new IapPurchaseService(wallet, ent, season, new FakeReceiptValidator(), clock);

            Assert.That(iap.CompletePurchase(IapCatalog.GemsMedium, "receipt-1", "order-1"),
                Is.EqualTo(PurchaseResult.Success));
            Assert.That(wallet.Gems, Is.EqualTo(320));

            Assert.That(iap.CompletePurchase(IapCatalog.GemsMedium, "receipt-1", "order-1"),
                Is.EqualTo(PurchaseResult.DuplicateOrder));
            Assert.That(wallet.Gems, Is.EqualTo(320));
        }

        [Test]
        public void Iap_DiamondPass_ActivatesSubscription_SeasonPremium_OwnsTrack()
        {
            var (clock, wallet) = Setup();
            var ent = new EntitlementService(null, clock);
            var season = new SeasonPassSystem(null, wallet, clock);
            var iap = new IapPurchaseService(wallet, ent, season, new FakeReceiptValidator(), clock);

            Assert.That(iap.CompletePurchase(IapCatalog.DiamondPass, "r", "o1"), Is.EqualTo(PurchaseResult.Success));
            Assert.That(ent.HasActiveSubscription(), Is.True);

            Assert.That(iap.CompletePurchase(IapCatalog.SeasonPremium, "r", "o2"), Is.EqualTo(PurchaseResult.Success));
            Assert.That(season.State.PremiumOwned, Is.True);
        }

        [Test]
        public void Iap_BadReceipt_AndUnknownProduct_Rejected()
        {
            var (clock, wallet) = Setup();
            var ent = new EntitlementService(null, clock);
            var season = new SeasonPassSystem(null, wallet, clock);
            var iap = new IapPurchaseService(wallet, ent, season, new FakeReceiptValidator(), clock);

            Assert.That(iap.CompletePurchase(IapCatalog.GemsSmall, "TAMPERED", "o"), Is.EqualTo(PurchaseResult.InvalidInput));
            Assert.That(iap.CompletePurchase("mystery_pack", "r", "o"), Is.EqualTo(PurchaseResult.UnknownItem));
            Assert.That(wallet.Gems, Is.EqualTo(0));
        }

        [Test]
        public void Migration_V1Save_UpgradesToV2_WithDefaults()
        {
            var v1 = new SaveData { SchemaVersion = SaveData.V1SchemaVersion };

            Assert.That(SaveClamp.MigrateToCurrent(v1), Is.True);
            Assert.That(v1.SchemaVersion, Is.EqualTo(SaveData.CurrentSchemaVersion));
            Assert.That(v1.WalletCoins, Is.EqualTo(0));
            Assert.That(v1.Ledger.Count, Is.EqualTo(0));
            Assert.That(v1.Missions, Is.Not.Null);
        }

        [Test]
        public void Migration_FutureVersion_Rejected()
        {
            var future = new SaveData { SchemaVersion = SaveData.CurrentSchemaVersion + 5 };

            Assert.That(SaveClamp.MigrateToCurrent(future), Is.False);
            Assert.That(SaveClamp.IsSupportedSchema(future.SchemaVersion), Is.False);
        }

        [Test]
        public void Clamp_SaveData_NegativeBalances_Zeroed_StreakCapped()
        {
            var d = new SaveData { WalletCoins = -100, WalletGems = -1, CurrentStreak = 999999, BestStreak = -4 };
            d.Missions.PlayCount = -8;
            d.Missions.AdBonusesToday = 99;

            SaveClamp.Clamp(d);

            Assert.That(d.WalletCoins, Is.EqualTo(0));
            Assert.That(d.WalletGems, Is.EqualTo(0));
            Assert.That(d.CurrentStreak, Is.EqualTo(9_999));
            Assert.That(d.BestStreak, Is.EqualTo(0));
            Assert.That(d.Missions.PlayCount, Is.EqualTo(0));
            Assert.That(d.Missions.AdBonusesToday, Is.EqualTo(MissionRules.MaxAdBonusesPerDay));
        }

        [Test]
        public void Integration_MatchRewards_FlowThroughAllSystems_LedgerVerified()
        {
            var (clock, wallet) = Setup();
            var missions = new DailyMissionSystem(null, wallet, clock);
            var season = new SeasonPassSystem(null, wallet, clock);
            var rewards = new MatchRewardService(wallet, missions, season, clock);
            var stats = new MatchStats();

            stats.Observe(new MatchEvent(MatchEventType.HitRecorded, 1, false));
            stats.Observe(new MatchEvent(MatchEventType.HitRecorded, 1, false));
            stats.Observe(new MatchEvent(MatchEventType.HomerunRecorded, 1, false));

            int xp = rewards.ApplyPostMatch(Winner.Home, stats);

            Assert.That(xp, Is.GreaterThan(0));
            Assert.That(wallet.Coins, Is.EqualTo(MatchRewardService.WinCoins));
            Assert.That(Wallet.VerifyChain(wallet.Entries, Key), Is.True);
            Assert.That(missions.ReadyMissionIds(), Does.Contain("hr_1"));

            long coinsBefore = wallet.Coins;
            long gemsBefore = wallet.Gems;
            rewards.ApplyPostMatch(Winner.Home, stats);
            foreach (var id in missions.ReadyMissionIds())
            {
                missions.Claim(id);
            }
            Assert.That(season.ClaimReward(1), Is.EqualTo(PurchaseResult.Success));

            Assert.That(wallet.Gems, Is.GreaterThan(gemsBefore));
            Assert.That(wallet.Coins, Is.GreaterThan(coinsBefore));
            Assert.That(Wallet.VerifyChain(wallet.Entries, Key), Is.True);
        }

        [Test]
        public void Fuzz_TenSeededGames_RewardsNeverBreakEconomy()
        {
            for (uint seed = 900; seed < 910; seed++)
            {
                var (clock, wallet) = Setup();
                var missions = new DailyMissionSystem(null, wallet, clock);
                var season = new SeasonPassSystem(null, wallet, clock);
                var rewards = new MatchRewardService(wallet, missions, season, clock);

                var engine = new MatchEngine(new TimingContactModel());
                AutoMatch.PlaySelfContained(engine, Difficulty.Normal, seed);

                var stats = new MatchStats();
                foreach (var e in engine.DrainEvents())
                {
                    stats.Observe(e);
                }

                int xp = 0;
                Assert.DoesNotThrow(() => xp = rewards.ApplyPostMatch(engine.State.Result, stats));

                Assert.That(xp, Is.InRange(0, SeasonRules.MaxDailyXp));
                Assert.That(season.State.DailyXpSpent, Is.LessThanOrEqualTo(SeasonRules.MaxDailyXp));
                Assert.That(season.State.Xp, Is.GreaterThanOrEqualTo(0));
                Assert.That(wallet.Coins, Is.GreaterThanOrEqualTo(0));
                Assert.That(wallet.Gems, Is.GreaterThanOrEqualTo(0));
                Assert.That(Wallet.VerifyChain(wallet.Entries, Key), Is.True);
            }
        }

        [Test]
        public void Fuzz_RealVolumes_CapsAndAttributionHold()
        {
            for (uint seed = 950; seed < 955; seed++)
            {
                var (clock, wallet) = Setup();
                var missions = new DailyMissionSystem(null, wallet, clock);
                var season = new SeasonPassSystem(null, wallet, clock);

                var engine = new MatchEngine(new TimingContactModel());
                AutoMatch.PlaySelfContained(engine, Difficulty.Normal, seed);

                var stats = new MatchStats();
                int bottomHalfHitEvents = 0;
                foreach (var e in engine.DrainEvents())
                {
                    stats.Observe(e);
                    if ((e.Type == MatchEventType.HitRecorded || e.Type == MatchEventType.HomerunRecorded) && !e.IsTop)
                        bottomHalfHitEvents++;
                }

                var rewards = new MatchRewardService(wallet, missions, season, clock);
                rewards.ApplyPostMatch(engine.State.Result, stats);

                Assert.That(season.State.DailyXpSpent, Is.LessThanOrEqualTo(SeasonRules.MaxDailyXp));
                Assert.That(season.State.Xp, Is.GreaterThanOrEqualTo(0));
                Assert.That(stats.HomeHits, Is.EqualTo(bottomHalfHitEvents),
                    "harness must measure the real simulated match volumes");
            }
        }
    }
}
