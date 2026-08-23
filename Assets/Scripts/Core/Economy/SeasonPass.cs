using System;
using System.Collections.Generic;

namespace DiamondTilt.Core.Economy
{
    [Serializable]
    public sealed class SeasonPassState
    {
        public string SeasonId = "";
        public int Xp;
        public int DailyXpSpent;
        public string DailyXpDay = "";
        public List<int> ClaimedFreeTiers = new List<int>();
        public List<int> ClaimedPremiumTiers = new List<int>();
        public bool PremiumOwned;
        public int SeasonsCompleted;
    }

    public static class SeasonRules
    {
        private static readonly SeasonConfig Default = SeasonConfig.Default;

        public static int Tiers => Default.Tiers;
        public static int XpPerTier => Default.XpPerTier;
        public static int MaxDailyXp => Default.MaxDailyXp;
        public static int WinXp => Default.WinXp;
        public static int LossXp => Default.LossXp;
        public static int PerHitXp => Default.PerHitXp;
        public static int PerHrXp => Default.PerHrXp;

        public static long FreeCoinReward(int tier) => Default.FreeCoinReward(tier);
        public static long PremiumGemReward(int tier) => Default.PremiumGemReward(tier);
        public static long PremiumCoinReward(int tier) => Default.PremiumCoinReward(tier);
    }

    public sealed class SeasonPassSystem
    {
        private readonly Wallet _wallet;
        private readonly IClock _clock;
        private readonly Func<bool> _premiumPersists;
        private readonly EconomyEventBus _bus;
        private readonly SeasonConfig _config;

        public SeasonPassState State { get; }

        public SeasonPassSystem(SeasonPassState state, Wallet wallet, IClock clock, Func<bool> premiumPersists = null, EconomyEventBus bus = null, SeasonConfig config = null)
        {
            State = state ?? new SeasonPassState();
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _premiumPersists = premiumPersists ?? (Func<bool>)(() => false);
            _bus = bus;
            _config = config ?? SeasonConfig.Default;
            EnsureSeason();
        }

        public void EnsureSeason()
        {
            string current = TimeKeys.SeasonId(_clock.UtcNow);
            if (State.SeasonId == current) return;

            bool hadPreviousSeason = !string.IsNullOrEmpty(State.SeasonId);
            bool keepPremium = State.PremiumOwned && _premiumPersists();
            State.SeasonId = current;
            State.Xp = 0;
            State.DailyXpSpent = 0;
            State.DailyXpDay = "";
            State.ClaimedFreeTiers.Clear();
            State.ClaimedPremiumTiers.Clear();
            State.PremiumOwned = keepPremium;
            if (hadPreviousSeason) State.SeasonsCompleted++;
        }

        public int RecordMatch(bool won, int hits, int homeruns)
        {
            EnsureSeason();
            ResetDailyWindowIfNeeded();

            hits = Math.Max(0, hits);
            homeruns = Math.Max(0, homeruns);

            int xp = won ? _config.WinXp : _config.LossXp;
            xp += hits * _config.PerHitXp;
            xp += homeruns * _config.PerHrXp;

            int room = _config.MaxDailyXp - State.DailyXpSpent;
            if (room <= 0) return 0;
            if (xp > room) xp = room;

            State.Xp += xp;
            State.DailyXpSpent += xp;
            _bus?.Publish(EconomyEventType.XpGained, $"season:{xp}");
            return xp;
        }

        public bool IsTierUnlocked(int tier)
            => tier >= 1 && tier <= _config.Tiers && State.Xp >= tier * _config.XpPerTier;

        public bool IsPremiumTier(int tier) => _config.IsPremiumTier(tier);

        public PurchaseResult ClaimReward(int tier)
        {
            if (!IsTierUnlocked(tier)) return PurchaseResult.InvalidInput;

            bool premium = IsPremiumTier(tier);
            var claimed = premium ? State.ClaimedPremiumTiers : State.ClaimedFreeTiers;
            if (claimed.Contains(tier)) return PurchaseResult.DuplicateOrder;
            if (premium && !State.PremiumOwned) return PurchaseResult.InsufficientFunds;

            if (premium)
            {
                _wallet.Grant(CurrencyType.Gems, _config.PremiumGemReward(tier), $"season:gem:t{tier}", _clock);
                _wallet.Grant(CurrencyType.Coins, _config.PremiumCoinReward(tier), $"season:coin:t{tier}", _clock);
                claimed.Add(tier);
            }
            else
            {
                _wallet.Grant(CurrencyType.Coins, _config.FreeCoinReward(tier), $"season:free:t{tier}", _clock);
                claimed.Add(tier);
            }
            return PurchaseResult.Success;
        }

        public void SetPremiumOwned(bool owned) => State.PremiumOwned = owned;


        private void ResetDailyWindowIfNeeded()
        {
            string day = TimeKeys.DayKey(_clock.UtcNow);
            if (State.DailyXpDay != day)
            {
                State.DailyXpDay = day;
                State.DailyXpSpent = 0;
            }
        }
    }
}
