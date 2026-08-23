using System;
using System.Collections.Generic;

namespace DiamondTilt.Core
{
    public sealed class DailyMissionState
    {
        public string DayKey { get; set; } = "";
        public int PlayCount { get; set; }
        public int HitCount { get; set; }
        public int HrCount { get; set; }
        public int WinCount { get; set; }
        public List<string> ClaimedIds { get; set; } = new List<string>();
        public int AdBonusesToday { get; set; }
    }

    public static class MissionRules
    {
        public const int MaxAdBonusesPerDay = 5;
        public const long AdBonusCoins = 150;

        public static readonly (string Id, string Desc, Func<DailyMissionState, bool> Done, long GemReward)[] Catalog =
        {
            ("play_2", "2 matches played", s => s.PlayCount >= 2, 2),
            ("hits_5", "5 hits collected", s => s.HitCount >= 5, 3),
            ("hr_1", "1 homerun", s => s.HrCount >= 1, 5),
            ("win_1", "Win a match", s => s.WinCount >= 1, 4),
        };
    }

    public sealed class DailyMissionSystem
    {
        private readonly Wallet _wallet;
        private readonly IClock _clock;

        public DailyMissionState State { get; }

        public DailyMissionSystem(DailyMissionState state, Wallet wallet, IClock clock)
        {
            State = state ?? new DailyMissionState();
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
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
            foreach (var (id, _, done, _) in MissionRules.Catalog)
            {
                if (done(State) && !State.ClaimedIds.Contains(id)) ready.Add(id);
            }
            return ready;
        }

        public PurchaseResult Claim(string missionId)
        {
            EnsureDay();
            foreach (var (id, _, done, reward) in MissionRules.Catalog)
            {
                if (id != missionId) continue;
                if (State.ClaimedIds.Contains(id)) return PurchaseResult.DuplicateOrder;
                if (!done(State)) return PurchaseResult.InvalidInput;

                _wallet.Grant(CurrencyType.Gems, reward, $"mission:{id}", _clock);
                State.ClaimedIds.Add(id);
                return PurchaseResult.Success;
            }
            return PurchaseResult.UnknownItem;
        }

        public PurchaseResult ClaimRewardedAdBonus()
        {
            EnsureDay();
            if (State.AdBonusesToday >= MissionRules.MaxAdBonusesPerDay) return PurchaseResult.InvalidInput;

            State.AdBonusesToday++;
            _wallet.Grant(CurrencyType.Coins, MissionRules.AdBonusCoins, "ad:bonus", _clock);
            return PurchaseResult.Success;
        }
    }
}
