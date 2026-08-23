using System;
using System.Linq;
using DiamondTilt.Core;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class EconomyEdgeTests
    {
        private static readonly DateTime BaseUtc = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
        private static readonly byte[] Key = SaveIntegrity.DeriveKey(64u);

        [Test]
        public void Wallet_CurrenciesIsolated()
        {
            var w = new Wallet(Key);
            var clock = new FixedClock(BaseUtc);

            w.Grant(CurrencyType.Coins, 100, "c", clock);

            Assert.That(w.Gems, Is.EqualTo(0));
            Assert.That(w.BalanceOf(CurrencyType.Gems), Is.EqualTo(0));
            Assert.That(w.BalanceOf(CurrencyType.Coins), Is.EqualTo(100));
        }

        [Test]
        public void Wallet_EntryDayKeys_TrackClockAcrossDays()
        {
            var w = new Wallet(Key);
            var clock = new FixedClock(BaseUtc);

            w.Grant(CurrencyType.Coins, 1, "day1", clock);
            clock.AdvanceDays(1);
            w.Grant(CurrencyType.Coins, 1, "day2", clock);

            Assert.That(w.Entries[0].DayKey, Is.EqualTo("2026-08-23"));
            Assert.That(w.Entries[1].DayKey, Is.EqualTo("2026-08-24"));
            Assert.That(Wallet.VerifyChain(w.Entries, Key), Is.True);
        }

        [Test]
        public void Wallet_VerifyChain_EmptyList_True()
        {
            Assert.That(Wallet.VerifyChain(new LedgerEntry[0], Key), Is.True);
        }

        [Test]
        public void Wallet_FromEntries_NullEntries_Throws()
        {
            Assert.Throws<EconomyException>(() => Wallet.FromEntries(Key, null));
        }

        [Test]
        public void Shop_CoinPricedItem_UsesCoins()
        {
            var w = new Wallet(Key, coins: 600);
            var shop = new PurchaseProcessor();

            var result = shop.TryPurchase("bat_lucky", "o1", w, new FixedClock(BaseUtc));

            Assert.That(result, Is.EqualTo(PurchaseResult.Success));
            Assert.That(w.Coins, Is.EqualTo(100));
            Assert.That(w.Gems, Is.EqualTo(0));
        }

        [Test]
        public void ShopCatalog_IdsUnique_AndPricesPositive()
        {
            var ids = ShopCatalog.All.Select(i => i.Id).ToList();

            Assert.That(ids.Count, Is.EqualTo(ids.Distinct().Count()));
            Assert.That(ShopCatalog.All.All(i => i.Price > 0), Is.True);
        }

        [Test]
        public void Season_Tier30_RequiresFullProgression_AcrossDays()
        {
            var clock = new FixedClock(BaseUtc);
            var season = new SeasonPassSystem(null, new Wallet(Key), clock);

            int daysNeeded = 0;
            while (!season.IsTierUnlocked(SeasonRules.Tiers) && daysNeeded < 40)
            {
                season.RecordMatch(true, 10, 2);
                clock.AdvanceDays(1);
                daysNeeded++;
            }

            Assert.That(season.IsTierUnlocked(SeasonRules.Tiers), Is.True);
            Assert.That(daysNeeded, Is.GreaterThanOrEqualTo(SeasonRules.Tiers * SeasonRules.XpPerTier / SeasonRules.MaxDailyXp));
        }

        [Test]
        public void Season_ClaimedTiersInvalidAfterRollover()
        {
            var clock = new FixedClock(BaseUtc);
            var season = new SeasonPassSystem(null, new Wallet(Key), clock);
            season.RecordMatch(true, 20, 0);
            season.ClaimReward(1);

            clock.AdvanceDays(40);
            season.EnsureSeason();
            season.RecordMatch(true, 20, 0);

            Assert.That(season.ClaimReward(1), Is.EqualTo(PurchaseResult.Success),
                "new season must allow claiming tier 1 again");
        }

        [Test]
        public void Season_Rewards_PositiveAndSane()
        {
            for (int tier = 1; tier <= SeasonRules.Tiers; tier++)
            {
                Assert.That(SeasonRules.FreeCoinReward(tier), Is.GreaterThan(0));
                Assert.That(SeasonRules.PremiumCoinReward(tier), Is.GreaterThan(0));
                if (tier % 5 == 0) Assert.That(SeasonRules.PremiumGemReward(tier), Is.GreaterThan(0));
            }
        }

        [Test]
        public void Missions_Catalog_IdsUnique()
        {
            var ids = MissionRules.Catalog.Select(m => m.Id).ToList();

            Assert.That(ids.Count, Is.EqualTo(ids.Distinct().Count()));
        }

        [Test]
        public void Missions_NegativeInputs_Ignored()
        {
            var clock = new FixedClock(BaseUtc);
            var missions = new DailyMissionSystem(null, new Wallet(Key), clock);

            missions.RecordMatch(true, hits: -5, homeruns: -2);

            Assert.That(missions.State.HitCount, Is.EqualTo(0));
            Assert.That(missions.State.HrCount, Is.EqualTo(0));
            Assert.That(missions.State.PlayCount, Is.EqualTo(1));
        }

        [Test]
        public void Entitlements_SameDayDoubleActivation_ExtendsNotShortens()
        {
            var clock = new FixedClock(BaseUtc);
            var ent = new EntitlementService(null, clock);

            ent.ActivateOrExtend(30);
            string firstExpiry = ent.State.ExpiryDayKey;

            ent.ActivateOrExtend(30);

            Assert.That(ent.State.ExpiryDayKey, Is.Not.EqualTo(firstExpiry));
            DateTime.Parse(ent.State.ExpiryDayKey);
            Assert.That(DateTime.Parse(ent.State.ExpiryDayKey),
                Is.GreaterThan(DateTime.Parse(firstExpiry)));
        }

        [Test]
        public void Iap_RestoredOrders_RemainIdempotent()
        {
            var clock = new FixedClock(BaseUtc);
            var wallet = new Wallet(Key);
            var ent = new EntitlementService(null, clock);
            var season = new SeasonPassSystem(null, wallet, clock);
            var iap = new IapPurchaseService(wallet, ent, season, new FakeReceiptValidator(), clock);

            Assert.That(iap.CompletePurchase(IapCatalog.GemsSmall, "r", "order-z"), Is.EqualTo(PurchaseResult.Success));

            var revived = new IapPurchaseService(wallet, ent, season, new FakeReceiptValidator(), clock);
            revived.RestoreOrders(iap.CompletedOrders);

            Assert.That(revived.CompletePurchase(IapCatalog.GemsSmall, "r", "order-z"),
                Is.EqualTo(PurchaseResult.DuplicateOrder));
            Assert.That(wallet.Gems, Is.EqualTo(60));
        }

        [Test]
        public void Rewards_LossPath_GrantsLossCoins()
        {
            var clock = new FixedClock(BaseUtc);
            var wallet = new Wallet(Key);
            var rewards = new MatchRewardService(wallet,
                new DailyMissionSystem(null, wallet, clock),
                new SeasonPassSystem(null, wallet, clock),
                clock);

            rewards.ApplyPostMatch(Winner.Away, new MatchStats());

            Assert.That(wallet.Coins, Is.EqualTo(MatchRewardService.LossCoins));
        }

        [Test]
        public void Rewards_NullStats_Throws()
        {
            var clock = new FixedClock(BaseUtc);
            var wallet = new Wallet(Key);
            var rewards = new MatchRewardService(wallet,
                new DailyMissionSystem(null, wallet, clock),
                new SeasonPassSystem(null, wallet, clock),
                clock);

            Assert.Throws<ArgumentNullException>(() => rewards.ApplyPostMatch(Winner.Home, null));
        }

        [Test]
        public void FixedClock_AdvanceDays_Works()
        {
            var clock = new FixedClock(BaseUtc);

            clock.AdvanceDays(7);

            Assert.That(clock.UtcNow, Is.EqualTo(BaseUtc.AddDays(7)));
        }

        [Test]
        public void TimeKeys_CultureInvariant_Formats()
        {
            var original = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");

                Assert.That(TimeKeys.DayKey(BaseUtc), Is.EqualTo("2026-08-23"));
                Assert.That(TimeKeys.SeasonId(BaseUtc), Is.EqualTo("2026-08"));
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = original;
            }
        }

        [Test]
        public void Entitlements_MalformedExpiryDayKey_FailsClosed_NoThrow()
        {
            var clock = new FixedClock(BaseUtc);
            var ent = new EntitlementService(new SubscriptionState { ExpiryDayKey = "99/99/9999" }, clock);

            Assert.That(ent.HasActiveSubscription(), Is.False);
            Assert.DoesNotThrow(() => ent.ActivateOrExtend(30));
            Assert.That(ent.HasActiveSubscription(), Is.True);
        }

        [Test]
        public void Shop_CosmeticRepurchase_BlockedWithoutCharge_BoosterRepeatAllowed()
        {
            var clock = new FixedClock(BaseUtc);
            var wallet = new Wallet(Key, coins: 5000, gems: 5000);
            var shop = new PurchaseProcessor();

            Assert.That(shop.TryPurchase("uniform_home", "o1", wallet, clock), Is.EqualTo(PurchaseResult.Success));
            long gemsAfterFirst = wallet.Gems;

            Assert.That(shop.TryPurchase("uniform_home", "o2", wallet, clock), Is.EqualTo(PurchaseResult.DuplicateOrder));
            Assert.That(wallet.Gems, Is.EqualTo(gemsAfterFirst));

            Assert.That(shop.TryPurchase("bat_lucky", "o3", wallet, clock), Is.EqualTo(PurchaseResult.Success));
            Assert.That(shop.TryPurchase("bat_lucky", "o4", wallet, clock), Is.EqualTo(PurchaseResult.Success));
            Assert.That(wallet.Coins, Is.EqualTo(4000));
        }
    }
}
