using System;
using DiamondTilt.Core;
using DiamondTilt.Core.Economy;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class PresentationLogicTests
    {
        private static readonly DateTime BaseUtc = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);
        private static readonly byte[] Key = SaveIntegrity.DeriveKey(707u);

        [Test]
        public void HudMapper_MapsMidGameState()
        {
            var engine = new MatchEngine(new TimingContactModel());
            engine.State.Inning = 2;
            engine.State.IsTop = false;
            engine.State.Balls = 2;
            engine.State.Strikes = 1;
            engine.State.Outs = 1;
            engine.State.SecondBase = true;
            engine.State.HomeRuns = 3;

            var hud = HudMapper.From(engine.State);

            Assert.That(hud.InningLabel, Is.EqualTo("2말"));
            Assert.That(hud.CountLabel, Is.EqualTo("2-1"));
            Assert.That(hud.ScoreLabel, Is.EqualTo("0 : 3"));
            Assert.That(hud.BaseRunnerCount, Is.EqualTo(1));
        }

        [Test]
        public void HudMapper_NullState_ReturnsBlankDefault()
        {
            var hud = HudMapper.From(null);

            Assert.That(hud.Inning, Is.EqualTo(1));
            Assert.That(hud.IsTop, Is.True);
            Assert.That(hud.Phase, Is.EqualTo(MatchPhase.InProgress));
        }

        [Test]
        public void HudMapper_FinishedMatch_KeepsFinalScore()
        {
            var engine = new MatchEngine(new TimingContactModel());
            for (int i = 0; i < 18; i++) MatchTestHarness.StrikeOutBatter(engine);

            var hud = HudMapper.From(engine.State);

            Assert.That(hud.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(hud.ScoreLabel, Is.EqualTo("0 : 0"));
        }
        [Test]
        public void GameServices_ResetProgress_ZeroesProgress_KeepsSubscriptionAndDifficulty()
        {
            var clock = new FixedClock(BaseUtc);
            var save = new SaveData();
            SaveClamp.MigrateToCurrent(save);
            save.DifficultyTier = (int)Difficulty.Hard;
            var services = new GameServices(save, Key, clock);

            services.Wallet.Grant(CurrencyType.Coins, 500, "r", clock);
            services.SeasonPass.RecordMatch(true, 5, 5);
            services.Entitlements.ActivateOrExtend(30);

            services.ResetProgress();

            Assert.That(services.Wallet.Coins, Is.EqualTo(0));
            Assert.That(services.SeasonPass.State.Xp, Is.EqualTo(0));
            Assert.That(services.Entitlements.HasActiveSubscription(), Is.True);
            Assert.That(save.Subscription.ExpiryDayKey, Is.Not.Empty);
            Assert.That(services.CurrentDifficulty, Is.EqualTo(Difficulty.Hard));
        }

        [Test]
        public void CurrentDifficulty_ClampedIntoValidRange()
        {
            var save = new SaveData();
            SaveClamp.MigrateToCurrent(save);
            save.DifficultyTier = 99;

            var services = new GameServices(save, Key, new FixedClock(BaseUtc));

            Assert.That(services.CurrentDifficulty, Is.EqualTo(Difficulty.Hard));
        }
    }
}
