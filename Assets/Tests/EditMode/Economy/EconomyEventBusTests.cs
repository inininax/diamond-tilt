using DiamondTilt.Core;
using DiamondTilt.Core.Economy;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class EconomyEventBusTests
    {
        private static readonly byte[] Key = SaveIntegrity.DeriveKey(505u);
        private static readonly System.DateTime BaseUtc = new System.DateTime(2026, 8, 23, 12, 0, 0, System.DateTimeKind.Utc);

        [Test]
        public void Wallet_PublishesGrantAndSpend_WithReasonSubject()
        {
            var bus = new EconomyEventBus();
            var clock = new FixedClock(BaseUtc);
            var wallet = new Wallet(Key, bus: bus);

            wallet.Grant(CurrencyType.Coins, 10, "match:win", clock);
            wallet.Spend(CurrencyType.Coins, 4, "shop:x", clock);

            var events = bus.Drain();
            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(events[0].Type, Is.EqualTo(EconomyEventType.BalanceGranted));
            Assert.That(events[0].Subject, Does.Contain("match:win"));
            Assert.That(events[1].Type, Is.EqualTo(EconomyEventType.BalanceSpent));

            Assert.That(bus.Drain(), Is.Empty);
        }

        [Test]
        public void FailedMutation_PublishesNothing()
        {
            var bus = new EconomyEventBus();
            var clock = new FixedClock(BaseUtc);
            var wallet = new Wallet(Key, bus: bus);

            Assert.Throws<EconomyException>(() => wallet.Spend(CurrencyType.Gems, 5, "nope", clock));

            Assert.That(bus.Drain(), Is.Empty);
        }

        [Test]
        public void SeasonPublishesXpGain()
        {
            var bus = new EconomyEventBus();
            var clock = new FixedClock(BaseUtc);
            var season = new SeasonPassSystem(null, new Wallet(Key), clock, bus: bus);

            season.RecordMatch(true, 2, 1);

            var events = bus.Drain();
            Assert.That(events, Has.Some.Matches<EconomyEvent>(e => e.Type == EconomyEventType.XpGained));
        }

        [Test]
        public void NullBus_ServicesWorkSilently()
        {
            var clock = new FixedClock(BaseUtc);
            var wallet = new Wallet(Key);

            Assert.DoesNotThrow(() => wallet.Grant(CurrencyType.Coins, 5, "quiet", clock));
            Assert.That(wallet.Coins, Is.EqualTo(5));
        }
    }
}
