using System.Linq;
using DiamondTilt.Core;
using DiamondTilt.Core.Economy;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class GameConfigTests
    {
        [Test]
        public void DefaultConfigs_ValidateClean()
        {
            Assert.That(GameConfigValidator.Validate(SeasonConfig.Default), Is.Empty);
            Assert.That(GameConfigValidator.Validate(MissionsConfig.DefaultStandard()), Is.Empty);
        }

        [Test]
        public void Validator_RejectsInvalidSeasonConfigs()
        {
            Assert.That(GameConfigValidator.Validate(new SeasonConfig { Tiers = 0 }), Is.Not.Empty);
            Assert.That(GameConfigValidator.Validate(new SeasonConfig { XpPerTier = 0 }), Is.Not.Empty);
            Assert.That(GameConfigValidator.Validate(new SeasonConfig { MaxDailyXp = 10, WinXp = 60 }), Is.Not.Empty);
            Assert.That(GameConfigValidator.Validate(new SeasonConfig { PerHitXp = -1 }), Is.Not.Empty);
        }

        [Test]
        public void Validator_RejectsDuplicateMissionIds_AndNegativeRewards()
        {
            var dup = new MissionsConfig
            {
                Catalog = new[]
                {
                    new MissionDefinition { Id = "same", GemReward = 1 },
                    new MissionDefinition { Id = "same", GemReward = 2 },
                },
            };
            var negative = new MissionsConfig
            {
                Catalog = new[] { new MissionDefinition { Id = "ok", GemReward = -3 } },
                MaxAdBonusesPerDay = -1,
            };

            Assert.That(GameConfigValidator.Validate(dup).Any(e => e.Contains("duplicate")), Is.True);
            Assert.That(GameConfigValidator.Validate(negative), Is.Not.Empty);
        }

        [Test]
        public void CustomSeasonConfig_FlowsThroughSystem()
        {
            var clock = new FixedClock(BaseUtc());
            var wallet = new Wallet(SaveIntegrity.DeriveKey(9u));
            var config = new SeasonConfig { Tiers = 2, XpPerTier = 10, WinXp = 10, LossXp = 5, MaxDailyXp = 50, FreeCoinPerTier = 7 };
            var season = new SeasonPassSystem(null, wallet, clock, config: config);

            season.RecordMatch(won: true, hits: 0, homeruns: 0);

            Assert.That(season.IsTierUnlocked(1), Is.True);
            Assert.That(season.ClaimReward(1), Is.EqualTo(PurchaseResult.Success));
            Assert.That(wallet.Coins, Is.EqualTo(7));
            Assert.That(season.IsTierUnlocked(2), Is.False);
        }

        [Test]
        public void CustomRewardsConfig_ChangesWinCoins()
        {
            var clock = new FixedClock(BaseUtc());
            var wallet = new Wallet(SaveIntegrity.DeriveKey(9u));
            var missions = new DailyMissionSystem(null, wallet, clock);
            var season = new SeasonPassSystem(null, wallet, clock);
            var rewards = new MatchRewardService(wallet, missions, season, clock,
                new RewardsConfig { WinCoins = 250, LossCoins = 10 });

            rewards.ApplyPostMatch(Winner.Home, new MatchStats());

            Assert.That(wallet.Coins, Is.EqualTo(250));
        }

        [Test]
        public void Validator_RejectsMissionMissingId()
        {
            var bad = new MissionsConfig
            {
                Catalog = new[] { new MissionDefinition { Id = "", GemReward = 1 } },
            };

            Assert.That(GameConfigValidator.Validate(bad).Any(e => e.Contains("missing id")), Is.True);
        }

        [Test]
        public void DefaultStandard_CachedSingleton_SameInstanceAcrossCalls()
        {
            Assert.That(MissionsConfig.DefaultStandard(), Is.SameAs(MissionsConfig.DefaultStandard()));
        }

        [Test]
        public void GameServices_EmptyLedgerWithScalars_FallsBackToScalarWallet()
        {
            var save = new SaveData();
            SaveClamp.MigrateToCurrent(save);
            save.WalletCoins = 77;
            save.WalletGems = 9;

            var services = new GameServices(save, SaveIntegrity.DeriveKey(2u), new FixedClock(BaseUtc()));

            Assert.That(services.Wallet.Coins, Is.EqualTo(77));
            Assert.That(services.Wallet.Gems, Is.EqualTo(9));
        }

        [Test]
        public void CustomMissionsConfig_ClaimableThroughSystem()
        {
            var clock = new FixedClock(BaseUtc());
            var wallet = new Wallet(SaveIntegrity.DeriveKey(9u));
            var config = MissionsConfig.DefaultStandard();
            var missions = new DailyMissionSystem(null, wallet, clock, config);
            missions.RecordMatch(true, 0, 0);
            missions.RecordMatch(true, 0, 0);

            Assert.That(missions.Claim("play_2"), Is.EqualTo(PurchaseResult.Success));
            Assert.That(wallet.Gems, Is.EqualTo(2));
        }

        private static System.DateTime BaseUtc() => new System.DateTime(2026, 8, 23, 12, 0, 0, System.DateTimeKind.Utc);
    }
}
