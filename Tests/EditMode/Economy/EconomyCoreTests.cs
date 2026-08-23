using System;
using System.Linq;
using System.Collections.Generic;
using DiamondTilt.Core;
using DiamondTilt.Core.Economy;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class EconomyCoreTests
    {
        private static readonly DateTime BaseUtc = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
        private static readonly byte[] Key = SaveIntegrity.DeriveKey(77u);

        private static FixedClock Clock() => new FixedClock(BaseUtc);

        [Test]
        public void TimeKeys_Formats()
        {
            Assert.That(TimeKeys.DayKey(BaseUtc), Is.EqualTo("2026-08-23"));
            Assert.That(TimeKeys.SeasonId(BaseUtc), Is.EqualTo("2026-08"));
            Assert.That(TimeKeys.Today(BaseUtc), Is.EqualTo(new DateTime(2026, 8, 23)));
        }

        [Test]
        public void Wallet_InitialBalances()
        {
            var w = new Wallet(Key, coins: 100, gems: 10);

            Assert.That(w.Coins, Is.EqualTo(100));
            Assert.That(w.Gems, Is.EqualTo(10));
            Assert.That(w.Entries.Count, Is.EqualTo(0));
        }

        [Test]
        public void Wallet_Grants_AppendVerifiableEntries()
        {
            var clock = Clock();
            var w = new Wallet(Key);
            w.Grant(CurrencyType.Coins, 50, "test:grant", clock);
            w.Spend(CurrencyType.Coins, 20, "test:spend", clock);

            Assert.That(w.Coins, Is.EqualTo(30));
            Assert.That(w.Entries.Count, Is.EqualTo(2));
            Assert.That(w.Entries[0].PrevHash, Is.EqualTo("0"));
            Assert.That(w.Entries[1].PrevHash, Is.EqualTo(w.Entries[0].Hash));
            Assert.That(Wallet.VerifyChain(w.Entries, Key), Is.True);
        }

        [Test]
        public void Wallet_InsufficientSpend_Throws_StateUnchanged()
        {
            var w = new Wallet(Key, coins: 10);
            var clock = Clock();

            Assert.Throws<EconomyException>(() => w.Spend(CurrencyType.Coins, 11, "too:much", clock));

            Assert.That(w.Coins, Is.EqualTo(10));
            Assert.That(w.Entries.Count, Is.EqualTo(0));
        }

        [Test]
        public void Wallet_RejectsInvalidMutations()
        {
            var w = new Wallet(Key);
            var clock = Clock();

            Assert.Throws<EconomyException>(() => w.Grant(CurrencyType.Gems, 0, "zero", clock));
            Assert.Throws<EconomyException>(() => w.Grant(CurrencyType.Gems, -5, "negative", clock));
            Assert.Throws<EconomyException>(() => w.Grant(CurrencyType.Gems, 5, "", clock));
            Assert.Throws<ArgumentNullException>(() => w.Grant(CurrencyType.Gems, 5, "no-clock", null));
        }

        [Test]
        public void Wallet_LedgerTampering_Detected()
        {
            var clock = Clock();
            var w = new Wallet(Key);
            w.Grant(CurrencyType.Gems, 30, "honest", clock);
            w.Spend(CurrencyType.Gems, 10, "spend", clock);

            var forged = new List<LedgerEntry>(w.Entries);
            forged[0] = new LedgerEntry
            {
                Seq = forged[0].Seq, Type = forged[0].Type, Currency = forged[0].Currency,
                Amount = 99999, BalanceAfter = forged[0].BalanceAfter, Reason = forged[0].Reason,
                DayKey = forged[0].DayKey, PrevHash = forged[0].PrevHash, Hash = forged[0].Hash,
            };

            Assert.That(Wallet.VerifyChain(forged, Key), Is.False);
        }

        [Test]
        public void Wallet_RestoreFromEntries_VerifiedChain()
        {
            var clock = Clock();
            var original = new Wallet(Key);
            original.Grant(CurrencyType.Coins, 500, "a", clock);
            original.Grant(CurrencyType.Gems, 25, "b", clock);
            original.Spend(CurrencyType.Gems, 5, "c", clock);

            var restored = Wallet.FromEntries(Key, original.Entries);

            Assert.That(restored.Coins, Is.EqualTo(original.Coins));
            Assert.That(restored.Gems, Is.EqualTo(original.Gems));
            Assert.That(restored.Entries.Count, Is.EqualTo(3));
        }

        [Test]
        public void Wallet_RestoreWithBrokenChain_Throws()
        {
            var entries = new List<LedgerEntry>
            {
                new LedgerEntry { Seq = 0, Type = 0, Currency = 0, Amount = 5, BalanceAfter = 5,
                    Reason = "x", DayKey = "2026-08-23", PrevHash = "0", Hash = "deadbeef" },
            };

            Assert.Throws<EconomyException>(() => Wallet.FromEntries(Key, entries));
        }

        [Test]
        public void Wallet_BalanceCap_PreventsOverflow()
        {
            var clock = Clock();
            var w = new Wallet(Key);
            w.Grant(CurrencyType.Coins, 999999999999L, "cap-test", clock);
            long before = w.Coins;
            w.Grant(CurrencyType.Coins, 999999999999L, "cap-test2", clock);

            Assert.That(before, Is.EqualTo(999999999999L));
            Assert.That(w.Coins, Is.LessThanOrEqualTo(1_000_000_000_000L));
        }

        [Test]
        public void Wallet_NegativeInitialBalance_Throws()
        {
            Assert.Throws<EconomyException>(() => new Wallet(Key, coins: -1));
        }

        [Test]
        public void Shop_PurchaseSuccess_Deducts_AndMarksOwned()
        {
            var clock = Clock();
            var w = new Wallet(Key, gems: 300);
            var shop = new PurchaseProcessor();

            var result = shop.TryPurchase("uniform_home", "order-1", w, clock);

            Assert.That(result, Is.EqualTo(PurchaseResult.Success));
            Assert.That(w.Gems, Is.EqualTo(200));
            Assert.That(shop.OwnedItems.Contains("uniform_home"), Is.True);
        }

        [Test]
        public void Shop_DuplicateOrder_NoDoubleCharge()
        {
            var clock = Clock();
            var w = new Wallet(Key, gems: 300);
            var shop = new PurchaseProcessor();
            shop.TryPurchase("uniform_home", "order-1", w, clock);

            var again = shop.TryPurchase("uniform_home", "order-1", w, clock);

            Assert.That(again, Is.EqualTo(PurchaseResult.DuplicateOrder));
            Assert.That(w.Gems, Is.EqualTo(200));
        }

        [Test]
        public void Shop_UnknownItem_Reported()
        {
            var clock = Clock();
            var w = new Wallet(Key, gems: 999);
            var shop = new PurchaseProcessor();

            Assert.That(shop.TryPurchase("nonexistent", "order-x", w, clock),
                Is.EqualTo(PurchaseResult.UnknownItem));
        }

        [Test]
        public void Shop_InsufficientFunds_Reported_WalletUnchanged()
        {
            var clock = Clock();
            var w = new Wallet(Key, gems: 50);
            var shop = new PurchaseProcessor();

            var result = shop.TryPurchase("stadium_sunset", "order-y", w, clock);

            Assert.That(result, Is.EqualTo(PurchaseResult.InsufficientFunds));
            Assert.That(w.Gems, Is.EqualTo(50));
            Assert.That(shop.CompletedOrders.Contains("order-y"), Is.False);
        }

        [Test]
        public void Shop_InvalidInput_Reported()
        {
            var clock = Clock();
            var w = new Wallet(Key);
            var shop = new PurchaseProcessor();

            Assert.That(shop.TryPurchase(null, "o", w, clock), Is.EqualTo(PurchaseResult.InvalidInput));
            Assert.That(shop.TryPurchase("bat_lucky", "", w, clock), Is.EqualTo(PurchaseResult.InvalidInput));
            Assert.That(shop.TryPurchase("bat_lucky", "o", null, clock), Is.EqualTo(PurchaseResult.InvalidInput));
        }

        [Test]
        public void Shop_RestoreState_PreservesOrderMemory()
        {
            var shop = new PurchaseProcessor();
            shop.RestoreState(new[] { "old-order" }, new[] { "bat_lucky" });

            var freshWallet = new Wallet(Key, gems: 100);
            Assert.That(shop.TryPurchase("bat_lucky", "old-order", freshWallet, Clock()),
                Is.EqualTo(PurchaseResult.DuplicateOrder));
        }
    }
}
