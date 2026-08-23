using System;
using DiamondTilt.Core.Economy;

namespace DiamondTilt.Core
{
    public sealed class GameServices
    {
        public IClock Clock { get; }
        public byte[] IntegrityKey { get; }
        public Wallet Wallet { get; }
        public PurchaseProcessor Shop { get; }
        public SeasonPassSystem SeasonPass { get; }
        public DailyMissionSystem Missions { get; }
        public EntitlementService Entitlements { get; }
        public IapPurchaseService Iap { get; }
        public MatchRewardService Rewards { get; }

        public GameServices(SaveData save, byte[] integrityKey, IClock clock, IReceiptValidator receiptValidator = null)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            IntegrityKey = integrityKey ?? throw new ArgumentNullException(nameof(integrityKey));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));

            var wallet = Wallet.FromEntries(integrityKey, save.Ledger);
            if (wallet.Entries.Count == 0 && (save.WalletCoins > 0 || save.WalletGems > 0))
                wallet = new Wallet(integrityKey, save.WalletCoins, save.WalletGems);

            Wallet = wallet;
            Shop = new PurchaseProcessor();
            Shop.RestoreState(save.PurchaseOrders, save.OwnedShopItems);
            SeasonPass = new SeasonPassSystem(save.SeasonPass, Wallet, clock);
            Missions = new DailyMissionSystem(save.Missions, Wallet, clock);
            Entitlements = new EntitlementService(save.Subscription, clock);
            Iap = new IapPurchaseService(Wallet, Entitlements, SeasonPass,
                receiptValidator ?? new FakeReceiptValidator(), clock);
            Rewards = new MatchRewardService(Wallet, Missions, SeasonPass, clock);
            Iap.RestoreOrders(save.IapOrders);
        }

        public void WriteBackTo(SaveData save)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));

            save.WalletCoins = Wallet.Coins;
            save.WalletGems = Wallet.Gems;
            save.Ledger = new System.Collections.Generic.List<LedgerEntry>(Wallet.Entries);
            save.PurchaseOrders = new System.Collections.Generic.List<string>(Shop.CompletedOrders);
            save.OwnedShopItems = new System.Collections.Generic.List<string>(Shop.OwnedItems);
            save.IapOrders = new System.Collections.Generic.List<string>(Iap.CompletedOrders);
        }
    }
}
