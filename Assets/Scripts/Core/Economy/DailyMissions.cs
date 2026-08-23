using System;
using System.Collections.Generic;

namespace DiamondTilt.Core.Economy
{
    [Serializable]
    public sealed class DailyMissionState
    {
        public string DayKey = "";
        public int PlayCount;
        public int HitCount;
        public int HrCount;
        public int WinCount;
        public List<string> ClaimedIds = new List<string>();
        public int AdBonusesToday;
    }

    public static class MissionRules
    {
        private static readonly MissionsConfig Default = MissionsConfig.DefaultStandard();

        public static int MaxAdBonusesPerDay => Default.MaxAdBonusesPerDay;
        public static long AdBonusCoins => Default.AdBonusCoins;
    }

    public sealed class DailyMissionSystem
    {
        private readonly Wallet _wallet;
        private readonly IClock _clock;
        private readonly MissionsConfig _config;

        public DailyMissionState State { get; }

        public DailyMissionSystem(DailyMissionState state, Wallet wallet, IClock clock, MissionsConfig config = null)
        {
            State = state ?? new DailyMissionState();
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _config = config ?? MissionsConfig.DefaultStandard();
            EnsureDay();
        }

        public void EnsureDay()
        {
            string day = TimeKeys.DayKey(_clock.UtcNow);
            if (State.DayKey == day) return;
            State.DayKey = day;
            State.PlayCount = 0;
            State.HitCount = 0;
            State.HrCount = 0;
            State.WinCount = 0;
            State.ClaimedIds.Clear();
            State.AdBonusesToday = 0;
        }

        public void RecordMatch(bool won, int hits, int homeruns)
        {
            EnsureDay();
            State.PlayCount++;
            if (won) State.WinCount++;
            State.HitCount += Math.Max(0, hits);
            State.HrCount += Math.Max(0, homeruns);
        }

        public IReadOnlyList<string> ReadyMissionIds()
        {
            var ready = new List<string>();
            foreach (var m in _config.Catalog)
            {
                if (m.IsComplete(State) && !State.ClaimedIds.Contains(m.Id)) ready.Add(m.Id);
            }
            return ready;
        }

        public PurchaseResult Claim(string missionId)
        {
            EnsureDay();
            foreach (var m in _config.Catalog)
            {
                if (m.Id != missionId) continue;
                if (State.ClaimedIds.Contains(m.Id)) return PurchaseResult.DuplicateOrder;
                if (!m.IsComplete(State)) return PurchaseResult.InvalidInput;

                _wallet.Grant(CurrencyType.Gems, m.GemReward, $"mission:{m.Id}", _clock);
                State.ClaimedIds.Add(m.Id);
                return PurchaseResult.Success;
            }
            return PurchaseResult.UnknownItem;
        }

        public PurchaseResult ClaimRewardedAdBonus()
        {
            EnsureDay();
            if (State.AdBonusesToday >= _config.MaxAdBonusesPerDay) return PurchaseResult.InvalidInput;

            State.AdBonusesToday++;
            _wallet.Grant(CurrencyType.Coins, _config.AdBonusCoins, "ad:bonus", _clock);
            return PurchaseResult.Success;
        }
    }
}
