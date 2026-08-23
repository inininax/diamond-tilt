using System;
using System.Collections.Generic;

namespace DiamondTilt.Core
{
    public sealed class SeasonPassState
    {
        public string SeasonId { get; set; } = "";
        public int Xp { get; set; }
        public int DailyXpSpent { get; set; }
        public string DailyXpDay { get; set; } = "";
        public List<int> ClaimedFreeTiers { get; set; } = new List<int>();
        public List<int> ClaimedPremiumTiers { get; set; } = new List<int>();
        public bool PremiumOwned { get; set; }
        public int SeasonsCompleted { get; set; }
    }

    public static class SeasonRules
    {
        public const int Tiers = 30;
        public const int XpPerTier = 100;
        public const int MaxDailyXp = 300;
        public const int WinXp = 60;
        public const int LossXp = 20;
        public const int PerHitXp = 2;
        public const int PerHrXp = 10;

        public static long FreeCoinReward(int tier) => 60L * tier;
        public static long PremiumGemReward(int tier) => tier % 5 == 0 ? 15L : 5L;
        public static long PremiumCoinReward(int tier) => 30L * tier;
    }

    public sealed class SeasonPassSystem
    {
        private readonly Wallet _wallet;
        private readonly IClock _clock;
        private readonly Func<bool> _premiumPersists;
        private readonly EconomyEventBus _bus;

        public SeasonPassState State { get; }

        public SeasonPassSystem(SeasonPassState state, Wallet wallet, IClock clock, Func<bool> premiumPersists = null, EconomyEventBus bus = null)
        {
            State = state ?? new SeasonPassState();
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _premiumPersists = premiumPersists ?? (Func<bool>)(() => false);
            _bus = bus;
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

            int xp = won ? SeasonRules.WinXp : SeasonRules.LossXp;
            xp += hits * SeasonRules.PerHitXp;
            xp += homeruns * SeasonRules.PerHrXp;

            int room = SeasonRules.MaxDailyXp - State.DailyXpSpent;
            if (room <= 0) return 0;
            if (xp > room) xp = room;

            State.Xp += xp;
            State.DailyXpSpent += xp;
            _bus?.Publish(EconomyEventType.XpGained, $"season:{xp}");
            return xp;
        }

        public bool IsTierUnlocked(int tier)
            => tier >= 1 && tier <= SeasonRules.Tiers && State.Xp >= tier * SeasonRules.XpPerTier;

        public bool IsPremiumTier(int tier) => tier % 5 == 0;

        public PurchaseResult ClaimReward(int tier)
        {
            if (!IsTierUnlocked(tier)) return PurchaseResult.InvalidInput;

            bool premium = IsPremiumTier(tier);
            var claimed = premium ? State.ClaimedPremiumTiers : State.ClaimedFreeTiers;
            if (claimed.Contains(tier)) return PurchaseResult.DuplicateOrder;
            if (premium && !State.PremiumOwned) return PurchaseResult.InsufficientFunds;

            if (premium)
            {
                _wallet.Grant(CurrencyType.Gems, SeasonRules.PremiumGemReward(tier), $"season:gem:t{tier}", _clock);
                _wallet.Grant(CurrencyType.Coins, SeasonRules.PremiumCoinReward(tier), $"season:coin:t{tier}", _clock);
                claimed.Add(tier);
            }
            else
            {
                _wallet.Grant(CurrencyType.Coins, SeasonRules.FreeCoinReward(tier), $"season:free:t{tier}", _clock);
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
