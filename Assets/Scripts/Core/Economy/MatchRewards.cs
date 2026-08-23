using System;

namespace DiamondTilt.Core.Economy
{
    public sealed class MatchRewardService
    {
        private readonly Wallet _wallet;
        private readonly DailyMissionSystem _missions;
        private readonly SeasonPassSystem _season;
        private readonly IClock _clock;

        public const long WinCoins = 100;
        public const long LossCoins = 30;

        private readonly RewardsConfig _config;

        public MatchRewardService(Wallet wallet, DailyMissionSystem missions, SeasonPassSystem season, IClock clock, RewardsConfig config = null)
        {
            _config = config ?? RewardsConfig.Default;
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _missions = missions ?? throw new ArgumentNullException(nameof(missions));
            _season = season ?? throw new ArgumentNullException(nameof(season));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public int ApplyPostMatch(Winner result, MatchStats stats)
        {
            if (stats == null) throw new ArgumentNullException(nameof(stats));

            bool playerWon = result == Winner.Home;
            _missions.RecordMatch(playerWon, stats.HomeHits, stats.HomeHomeruns);

            long coins = playerWon ? _config.WinCoins : _config.LossCoins;
            _wallet.Grant(CurrencyType.Coins, coins, $"match:{(playerWon ? "win" : "loss")}", _clock);

            return _season.RecordMatch(playerWon, stats.HomeHits, stats.HomeHomeruns);
        }
    }
}
