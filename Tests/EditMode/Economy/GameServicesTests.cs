using System;
using DiamondTilt.Core;
using DiamondTilt.Core.Economy;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class GameServicesTests
    {
        private static readonly DateTime BaseUtc = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
        private static readonly byte[] Key = SaveIntegrity.DeriveKey(606u);

        [Test]
        public void Compose_FromFreshSave_AllSystemsOperational()
        {
            var save = new SaveData();
            SaveClamp.MigrateToCurrent(save);

            var services = new GameServices(save, Key, new FixedClock(BaseUtc));

            services.Wallet.Grant(CurrencyType.Coins, 500, "boot", services.Clock);
            Assert.That(services.Rewards, Is.Not.Null);
            Assert.That(services.Iap.CompletePurchase(IapCatalog.GemsSmall, "r", "o1"),
                Is.EqualTo(PurchaseResult.Success));
            Assert.That(services.Wallet.Gems, Is.EqualTo(60));
        }

        [Test]
        public void WriteBack_CapturesLedgerAndOrders_RoundTripRestores()
        {
            var save = new SaveData();
            SaveClamp.MigrateToCurrent(save);
            var services = new GameServices(save, Key, new FixedClock(BaseUtc));

            services.Wallet.Grant(CurrencyType.Gems, 100, "r", services.Clock);
            services.Shop.TryPurchase("uniform_home", "ord-1", services.Wallet, services.Clock);
            services.WriteBackTo(save);

            var revived = new GameServices(save, Key, new FixedClock(BaseUtc));

            Assert.That(revived.Wallet.Gems, Is.EqualTo(0));
            Assert.That(revived.Shop.TryPurchase("uniform_home", "ord-1", revived.Wallet, new FixedClock(BaseUtc)),
                Is.EqualTo(PurchaseResult.DuplicateOrder));
        }

        [Test]
        public void NullArguments_Rejected()
        {
            var save = new SaveData();
            Assert.Throws<ArgumentNullException>(() => new GameServices(null, Key, new FixedClock(BaseUtc)));
            Assert.Throws<ArgumentNullException>(() => new GameServices(save, null, new FixedClock(BaseUtc)));
            Assert.Throws<ArgumentNullException>(() => new GameServices(save, Key, null));
        }

        [Test]
        public void CustomReceiptValidator_IsHonored()
        {
            var save = new SaveData();
            SaveClamp.MigrateToCurrent(save);
            var strict = new RejectingValidator();
            var services = new GameServices(save, Key, new FixedClock(BaseUtc), strict);

            Assert.That(services.Iap.CompletePurchase(IapCatalog.GemsSmall, "any", "o"),
                Is.EqualTo(PurchaseResult.InvalidInput));
        }

        private sealed class RejectingValidator : IReceiptValidator
        {
            public bool Validate(string productId, string receiptPayload) => false;
        }
    }
}
