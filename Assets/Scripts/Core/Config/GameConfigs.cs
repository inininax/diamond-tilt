using System;
using System.Collections.Generic;
using DiamondTilt.Core.Economy;

namespace DiamondTilt.Core
{
    public sealed class SeasonConfig
    {
        public int Tiers { get; init; } = 30;
        public int XpPerTier { get; init; } = 100;
        public int MaxDailyXp { get; init; } = 300;
        public int WinXp { get; init; } = 60;
        public int LossXp { get; init; } = 20;
        public int PerHitXp { get; init; } = 2;
        public int PerHrXp { get; init; } = 10;
        public long FreeCoinPerTier { get; init; } = 60;
        public long PremiumCoinPerTier { get; init; } = 30;
        public long PremiumGemMilestone { get; init; } = 15;
        public long PremiumGemBase { get; init; } = 5;

        public long FreeCoinReward(int tier) => FreeCoinPerTier * tier;
        public long PremiumCoinReward(int tier) => PremiumCoinPerTier * tier;
        public bool IsPremiumTier(int tier) => tier % 5 == 0;
        public long PremiumGemReward(int tier) => IsPremiumTier(tier) ? PremiumGemMilestone : PremiumGemBase;

        public static SeasonConfig Default { get; } = new SeasonConfig();
    }

    public sealed class RewardsConfig
    {
        public long WinCoins { get; init; } = 100;
        public long LossCoins { get; init; } = 30;

        public static RewardsConfig Default { get; } = new RewardsConfig();
    }

    public sealed class MissionDefinition
    {
        public string Id { get; init; } = "";
        public string Description { get; init; } = "";
        public Func<DailyMissionState, bool> IsComplete { get; init; } = _ => false;
        public long GemReward { get; init; }
    }

    public sealed class MissionsConfig
    {
        public int MaxAdBonusesPerDay { get; init; } = 5;
        public long AdBonusCoins { get; init; } = 150;
        public IReadOnlyList<MissionDefinition> Catalog { get; init; } = Array.Empty<MissionDefinition>();

        private static readonly MissionsConfig Standard = BuildStandard();

        public static MissionsConfig DefaultStandard() => Standard;

        private static MissionsConfig BuildStandard()
            => new MissionsConfig
            {
                Catalog = new[]
                {
                    new MissionDefinition { Id = "play_2", Description = "2 matches played", IsComplete = s => s.PlayCount >= 2, GemReward = 2 },
                    new MissionDefinition { Id = "hits_5", Description = "5 hits collected", IsComplete = s => s.HitCount >= 5, GemReward = 3 },
                    new MissionDefinition { Id = "hr_1", Description = "1 homerun", IsComplete = s => s.HrCount >= 1, GemReward = 5 },
                    new MissionDefinition { Id = "win_1", Description = "Win a match", IsComplete = s => s.WinCount >= 1, GemReward = 4 },
                },
            };
    }

    public static class GameConfigValidator
    {
        public static IReadOnlyList<string> Validate(SeasonConfig c)
        {
            var errors = new List<string>();
            if (c == null) return new[] { "season config null" };
            if (c.Tiers < 1 || c.Tiers > 100) errors.Add("tiers out of range");
            if (c.XpPerTier <= 0) errors.Add("xpPerTier must be positive");
            if (c.MaxDailyXp < c.WinXp) errors.Add("daily cap below single win");
            if (c.LossXp < 0 || c.PerHitXp < 0 || c.PerHrXp < 0) errors.Add("negative xp sources");
            if (c.FreeCoinPerTier < 0 || c.PremiumCoinPerTier < 0) errors.Add("negative coin rewards");
            if (c.PremiumGemBase < 0 || c.PremiumGemMilestone < 0) errors.Add("negative gem rewards");
            return errors;
        }

        public static IReadOnlyList<string> Validate(MissionsConfig c)
        {
            var errors = new List<string>();
            if (c == null) return new[] { "missions config null" };
            if (c.MaxAdBonusesPerDay < 0) errors.Add("negative ad cap");
            if (c.AdBonusCoins < 0) errors.Add("negative ad reward");
            var ids = new HashSet<string>();
            foreach (var m in c.Catalog)
            {
                if (string.IsNullOrEmpty(m.Id)) errors.Add("mission missing id");
                else if (!ids.Add(m.Id)) errors.Add($"duplicate mission id {m.Id}");
                if (m.GemReward < 0) errors.Add($"negative gem reward on {m.Id}");
            }
            return errors;
        }
    }
}
