using System;
using System.Collections.Generic;

namespace DiamondTilt.Core
{
    public sealed class MatchSnapshot
    {
        public int Inning { get; set; }
        public bool IsTop { get; set; }
        public int Balls { get; set; }
        public int Strikes { get; set; }
        public int Outs { get; set; }
        public bool FirstBase { get; set; }
        public bool SecondBase { get; set; }
        public bool ThirdBase { get; set; }
        public int AwayRuns { get; set; }
        public int HomeRuns { get; set; }
        public int Phase { get; set; }
        public int Result { get; set; }
    }

    public sealed class SaveData
    {
        public const int V1SchemaVersion = 1;
        public const int CurrentSchemaVersion = 2;

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public MatchSnapshot Match { get; set; } = new MatchSnapshot();
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int DifficultyTier { get; set; }
        public int CurrentStreak { get; set; }
        public int BestStreak { get; set; }

        public long WalletCoins { get; set; }
        public long WalletGems { get; set; }
        public List<LedgerEntry> Ledger { get; set; } = new List<LedgerEntry>();
        public SeasonPassState SeasonPass { get; set; } = new SeasonPassState();
        public DailyMissionState Missions { get; set; } = new DailyMissionState();
        public SubscriptionState Subscription { get; set; } = new SubscriptionState();
        public List<string> PurchaseOrders { get; set; } = new List<string>();
        public List<string> IapOrders { get; set; } = new List<string>();
        public List<string> OwnedShopItems { get; set; } = new List<string>();
    }

    public static class SaveClamp
    {
        private const long MaxBalance = 1_000_000_000_000;

        public static void Clamp(MatchSnapshot m)
        {
            if (m == null) return;
            m.Inning = ClampRange(m.Inning, 1, MatchState.Innings);
            m.Balls = ClampRange(m.Balls, 0, MatchState.BallsForWalk - 1);
            m.Strikes = ClampRange(m.Strikes, 0, MatchState.StrikesForOut - 1);
            int maxOuts = m.Phase == (int)MatchPhase.InProgress ? MatchState.StrikesForOut - 1 : MatchState.StrikesForOut;
            m.Outs = ClampRange(m.Outs, 0, maxOuts);
            m.AwayRuns = ClampRange(m.AwayRuns, 0, 999);
            m.HomeRuns = ClampRange(m.HomeRuns, 0, 999);
            if (m.Phase != (int)MatchPhase.Finished) m.Phase = (int)MatchPhase.InProgress;
            if (m.Result < (int)Winner.Away || m.Result > (int)Winner.Draw) m.Result = (int)Winner.Away;
        }

        public static void NormalizeSubscription(SubscriptionState s)
        {
            if (s == null) return;
            if (!TimeKeys.TryParseDayKey(s.ExpiryDayKey, out var expiry)
                || expiry.Year < 2020 || expiry.Year > 2100)
            {
                s.ExpiryDayKey = "";
            }
        }

        public static void Clamp(SaveData d)
        {
            if (d == null) return;

            d.WalletCoins = ClampRange(d.WalletCoins, 0, MaxBalance);
            d.WalletGems = ClampRange(d.WalletGems, 0, MaxBalance);
            d.Wins = ClampRange(d.Wins, 0, 999_999);
            d.Losses = ClampRange(d.Losses, 0, 999_999);
            d.CurrentStreak = ClampRange(d.CurrentStreak, 0, 9_999);
            d.BestStreak = ClampRange(d.BestStreak, 0, 9_999);
            d.DifficultyTier = ClampRange(d.DifficultyTier, GameSettings.MinDifficulty, GameSettings.MaxDifficulty);

            if (d.Ledger == null) d.Ledger = new List<LedgerEntry>();
            foreach (var e in d.Ledger)
            {
                if (e == null) continue;
                e.Amount = ClampRange(e.Amount, 0, MaxBalance);
                e.BalanceAfter = ClampRange(e.BalanceAfter, 0, MaxBalance);
                if (e.Type != (int)LedgerEntryType.Grant && e.Type != (int)LedgerEntryType.Spend) e.Type = (int)LedgerEntryType.Grant;
                if (e.Currency != (int)CurrencyType.Coins && e.Currency != (int)CurrencyType.Gems) e.Currency = (int)CurrencyType.Coins;
            }

            Normalize(d.SeasonPass);
            NormalizeSubscription(d.Subscription);
            if (d.Missions != null)
            {
                d.Missions.PlayCount = ClampRange(d.Missions.PlayCount, 0, 99);
                d.Missions.HitCount = ClampRange(d.Missions.HitCount, 0, 999);
                d.Missions.HrCount = ClampRange(d.Missions.HrCount, 0, 99);
                d.Missions.WinCount = ClampRange(d.Missions.WinCount, 0, 99);
                d.Missions.AdBonusesToday = ClampRange(d.Missions.AdBonusesToday, 0, MissionRules.MaxAdBonusesPerDay);
            }
        }

        private static void Normalize(SeasonPassState s)
        {
            if (s == null) return;
            s.Xp = ClampRange(s.Xp, 0, 999_999);
            s.DailyXpSpent = ClampRange(s.DailyXpSpent, 0, SeasonRules.MaxDailyXp);
            s.SeasonsCompleted = ClampRange(s.SeasonsCompleted, 0, 9_999);
            if (s.ClaimedFreeTiers == null) s.ClaimedFreeTiers = new List<int>();
            if (s.ClaimedPremiumTiers == null) s.ClaimedPremiumTiers = new List<int>();
            s.ClaimedFreeTiers.RemoveAll(t => t < 1 || t > SeasonRules.Tiers);
            s.ClaimedPremiumTiers.RemoveAll(t => t < 1 || t > SeasonRules.Tiers);
        }

        public static bool IsKnownSchema(int schemaVersion)
            => schemaVersion >= SaveData.V1SchemaVersion && schemaVersion <= SaveData.CurrentSchemaVersion;

        public static bool IsSupportedSchema(int schemaVersion)
            => schemaVersion == SaveData.CurrentSchemaVersion;

        public static bool MigrateToCurrent(SaveData d)
        {
            if (d == null) return false;
            if (d.SchemaVersion > SaveData.CurrentSchemaVersion) return false;
            if (d.SchemaVersion < SaveData.V1SchemaVersion) return false;

            if (d.SchemaVersion == SaveData.V1SchemaVersion)
            {
                d.WalletCoins = 0;
                d.WalletGems = 0;
                d.Ledger = new List<LedgerEntry>();
                d.SeasonPass = new SeasonPassState();
                d.Missions = new DailyMissionState();
                d.Subscription = new SubscriptionState();
                d.PurchaseOrders = new List<string>();
                d.IapOrders = new List<string>();
                d.OwnedShopItems = new List<string>();
                d.CurrentStreak = 0;
                d.BestStreak = 0;
                d.SchemaVersion = SaveData.CurrentSchemaVersion;
            }

            NormalizeEconomyDefaults(d);
            return true;
        }

        private static void NormalizeEconomyDefaults(SaveData d)
        {
            if (d.SeasonPass == null) d.SeasonPass = new SeasonPassState();
            if (d.Missions == null) d.Missions = new DailyMissionState();
            if (d.Subscription == null) d.Subscription = new SubscriptionState();
            if (d.Ledger == null) d.Ledger = new List<LedgerEntry>();
            if (d.PurchaseOrders == null) d.PurchaseOrders = new List<string>();
            if (d.IapOrders == null) d.IapOrders = new List<string>();
            if (d.OwnedShopItems == null) d.OwnedShopItems = new List<string>();
        }

        private static long ClampRange(long v, long min, long max)
            => v < min ? min : v > max ? max : v;

        private static int ClampRange(int v, int min, int max)
            => v < min ? min : v > max ? max : v;
    }

    public static class MatchStateIo
    {
        public static MatchSnapshot ToSnapshot(this MatchState s)
        {
            return new MatchSnapshot
            {
                Inning = s.Inning,
                IsTop = s.IsTop,
                Balls = s.Balls,
                Strikes = s.Strikes,
                Outs = s.Outs,
                FirstBase = s.FirstBase,
                SecondBase = s.SecondBase,
                ThirdBase = s.ThirdBase,
                AwayRuns = s.AwayRuns,
                HomeRuns = s.HomeRuns,
                Phase = (int)s.Phase,
                Result = (int)s.Result,
            };
        }

        public static void Restore(this MatchState state, MatchSnapshot snapshot)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            SaveClamp.Clamp(snapshot);
            state.Inning = snapshot.Inning;
            state.IsTop = snapshot.IsTop;
            state.Balls = snapshot.Balls;
            state.Strikes = snapshot.Strikes;
            state.Outs = snapshot.Outs;
            state.FirstBase = snapshot.FirstBase;
            state.SecondBase = snapshot.SecondBase;
            state.ThirdBase = snapshot.ThirdBase;
            state.AwayRuns = snapshot.AwayRuns;
            state.HomeRuns = snapshot.HomeRuns;
            state.Phase = (MatchPhase)snapshot.Phase;
            state.Result = (Winner)snapshot.Result;
        }
    }
}
