using System;
using DiamondTilt.Core.Economy;

namespace DiamondTilt.Core
{
    public sealed class GameServices
    {
        public IClock Clock { get; }
        public byte[] IntegrityKey { get; }
        public Wallet Wallet { get; private set; }
        public PurchaseProcessor Shop { get; private set; }
        public SeasonPassSystem SeasonPass { get; private set; }
        public DailyMissionSystem Missions { get; private set; }
        public EntitlementService Entitlements { get; private set; }
        public IapPurchaseService Iap { get; private set; }
        public MatchRewardService Rewards { get; private set; }
        public Difficulty CurrentDifficulty => (Difficulty)System.Math.Clamp(_sourceSave.DifficultyTier,
            GameSettings.MinDifficulty, GameSettings.MaxDifficulty);

        private readonly SaveData _sourceSave;
        private readonly IReceiptValidator _receiptValidator;

        public event Action SaveRequested;

        public void RequestManualSave() => SaveRequested?.Invoke();

        public static SaveData CreateFreshPayload()
        {
            var fresh = new SaveData();
            SaveClamp.MigrateToCurrent(fresh);
            SaveClamp.Clamp(fresh.Match);
            SaveClamp.Clamp(fresh);
            return fresh;
        }

        public void ResetProgress()
        {
            var fresh = CreateFreshPayload();
            SubscriptionState paidEntitlement = Entitlements.State;

            _sourceSave.WalletCoins = fresh.WalletCoins;
            _sourceSave.WalletGems = fresh.WalletGems;
            _sourceSave.Ledger = fresh.Ledger;
            _sourceSave.SeasonPass = fresh.SeasonPass;
            _sourceSave.Missions = fresh.Missions;
            _sourceSave.Subscription = paidEntitlement;
            _sourceSave.PurchaseOrders = fresh.PurchaseOrders;
            _sourceSave.IapOrders = fresh.IapOrders;
            _sourceSave.OwnedShopItems = fresh.OwnedShopItems;
            _sourceSave.CurrentStreak = 0;
            _sourceSave.BestStreak = 0;
            RecomposeFrom(_sourceSave);
        }

        public GameServices(SaveData save, byte[] integrityKey, IClock clock, IReceiptValidator receiptValidator = null)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));
            IntegrityKey = integrityKey ?? throw new ArgumentNullException(nameof(integrityKey));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _sourceSave = save;
            _receiptValidator = receiptValidator;

            RecomposeFrom(save);
        }

        private void RecomposeFrom(SaveData save)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));

            var wallet = Wallet.FromEntries(IntegrityKey, save.Ledger);
            if (wallet.Entries.Count == 0 && (save.WalletCoins > 0 || save.WalletGems > 0))
                wallet = new Wallet(IntegrityKey, save.WalletCoins, save.WalletGems);

            Wallet = wallet;
            Shop = new PurchaseProcessor();
            Shop.RestoreState(save.PurchaseOrders, save.OwnedShopItems);
            SeasonPass = new SeasonPassSystem(save.SeasonPass, Wallet, Clock);
            Missions = new DailyMissionSystem(save.Missions, Wallet, Clock);
            Entitlements = new EntitlementService(save.Subscription, Clock);
            Iap = new IapPurchaseService(Wallet, Entitlements, SeasonPass,
                _receiptValidator ?? new FakeReceiptValidator(), Clock);
            Rewards = new MatchRewardService(Wallet, Missions, SeasonPass, Clock);
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
            save.SeasonPass = SeasonPass.State;
            save.Missions = Missions.State;
            save.Subscription = Entitlements.State;
        }
    }
}
