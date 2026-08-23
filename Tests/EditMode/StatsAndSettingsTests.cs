using DiamondTilt.Core;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class StatsAndSettingsTests
    {
        [Test]
        public void Stats_HitsCounted_PerSide()
        {
            var stats = new MatchStats();
            stats.Observe(new MatchEvent(MatchEventType.HitRecorded, 1, true));
            stats.Observe(new MatchEvent(MatchEventType.HitRecorded, 1, true));
            stats.Observe(new MatchEvent(MatchEventType.HitRecorded, 1, false));

            Assert.That(stats.AwayHits, Is.EqualTo(2));
            Assert.That(stats.HomeHits, Is.EqualTo(1));
        }

        [Test]
        public void Stats_WalksAreNotHits()
        {
            var stats = new MatchStats();
            stats.Observe(new MatchEvent(MatchEventType.BatterWalked, 1, true));

            Assert.That(stats.AwayHits, Is.EqualTo(0));
        }

        [Test]
        public void Stats_Homerun_CountsAsHitAndHomerun()
        {
            var stats = new MatchStats();
            stats.Observe(new MatchEvent(MatchEventType.HomerunRecorded, 2, false));

            Assert.That(stats.HomeHits, Is.EqualTo(1));
            Assert.That(stats.HomeHomeruns, Is.EqualTo(1));
            Assert.That(stats.AwayHomeruns, Is.EqualTo(0));
        }

        [Test]
        public void Stats_StrikeoutsAttributedToBattingSide()
        {
            var stats = new MatchStats();
            stats.Observe(new MatchEvent(MatchEventType.BatterStruckOut, 3, true));
            stats.Observe(new MatchEvent(MatchEventType.BatterStruckOut, 3, false));

            Assert.That(stats.AwayStrikeouts, Is.EqualTo(1));
            Assert.That(stats.HomeStrikeouts, Is.EqualTo(1));
        }

        [Test]
        public void Engine_HitsEmitEvents_DuringRealPlay()
        {
            var engine = new MatchEngine(new TimingContactModel());
            AutoMatch.PlaySelfContained(engine, Difficulty.Normal, 31u);

            int hits = 0, hrs = 0;
            foreach (var e in engine.DrainEvents())
            {
                if (e.Type == MatchEventType.HitRecorded) hits++;
                if (e.Type == MatchEventType.HomerunRecorded) hrs++;
            }
            Assert.That(hits + hrs, Is.GreaterThan(0));
        }

        [Test]
        public void Streak_AccumulatesAndTracksBest_ResetsOnLoss_DrawNeutral()
        {
            var streak = new StreakTracker();
            streak.RecordPlayerResult(Winner.Home);
            streak.RecordPlayerResult(Winner.Home);
            streak.RecordPlayerResult(Winner.Draw);
            streak.RecordPlayerResult(Winner.Home);

            Assert.That(streak.CurrentWinStreak, Is.EqualTo(3));
            Assert.That(streak.BestWinStreak, Is.EqualTo(3));
            Assert.That(streak.Wins, Is.EqualTo(3));

            streak.RecordPlayerResult(Winner.Away);
            Assert.That(streak.CurrentWinStreak, Is.EqualTo(0));
            Assert.That(streak.BestWinStreak, Is.EqualTo(3));
            Assert.That(streak.Losses, Is.EqualTo(1));
        }

        [Test]
        public void Settings_Clamp_BringsDifficultyIntoRange()
        {
            var low = GameSettings.Clamp(new GameSettings { DifficultyTier = -4 });
            var high = GameSettings.Clamp(new GameSettings { DifficultyTier = 9 });
            var ok = GameSettings.Clamp(new GameSettings { DifficultyTier = (int)Difficulty.Hard });

            Assert.That(low.DifficultyTier, Is.EqualTo(GameSettings.MinDifficulty));
            Assert.That(high.DifficultyTier, Is.EqualTo(GameSettings.MaxDifficulty));
            Assert.That(ok.DifficultyTier, Is.EqualTo((int)Difficulty.Hard));
        }

        [Test]
        public void Settings_Clamp_NullReturnsDefaults()
        {
            var s = GameSettings.Clamp(null);

            Assert.That(s.DifficultyTier, Is.EqualTo((int)Difficulty.Normal));
            Assert.That(s.SoundEnabled, Is.True);
        }
    }
}
