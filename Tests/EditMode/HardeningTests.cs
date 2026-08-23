using System;
using DiamondTilt.Core;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class HardeningTests
    {
        [Test]
        public void BallFlight_HangTime_UnderDrag_ShorterOrEqual()
        {
            var launch = new LaunchParams(40, 35, 0);

            Assert.That(BallFlight.IntegrateWithDrag(launch).FlightTimeSeconds,
                Is.LessThanOrEqualTo(BallFlight.FlightTimeNoDrag(launch) + 0.01));
        }

        [Test]
        public void LaunchParams_Boundaries_Accepted()
        {
            Assert.DoesNotThrow(() => new LaunchParams(80, 90, -90));
            Assert.DoesNotThrow(() => new LaunchParams(1, 0, 90));
        }

        [Test]
        public void Vec3_ZeroVector_LengthSafe()
        {
            Assert.That(default(Vec3).Length(), Is.EqualTo(0));
            Assert.That(default(Vec3).HorizontalDistance(), Is.EqualTo(0));
        }

        [Test]
        public void SaveClamp_NullSnapshot_NoThrow()
        {
            Assert.DoesNotThrow(() => SaveClamp.Clamp(null));
        }

        [Test]
        public void Streak_BestPersistsAcrossLaterReset()
        {
            var streak = new StreakTracker();
            for (int i = 0; i < 4; i++) streak.RecordPlayerResult(Winner.Home);
            streak.RecordPlayerResult(Winner.Away);
            streak.RecordPlayerResult(Winner.Home);
            streak.RecordPlayerResult(Winner.Away);

            Assert.That(streak.BestWinStreak, Is.EqualTo(4));
            Assert.That(streak.CurrentWinStreak, Is.EqualTo(0));
        }

        [Test]
        public void AutoMatch_Guard_TerminatesOnStuckEngine()
        {
            var engine = new MatchEngine(new AlwaysFoulResolver());

            Assert.DoesNotThrow(() => AutoMatch.PlaySelfContained(engine, Difficulty.Normal, 3u));
            Assert.That(engine.State.Phase == MatchPhase.InProgress || engine.State.Phase == MatchPhase.Finished, Is.True);
        }

        [Test]
        public void WeightedContactResolver_NullModel_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new WeightedContactResolver(null));
        }

        [Test]
        public void DeriveKey_Deterministic_AndSeedSensitive()
        {
            var a1 = SaveIntegrity.DeriveKey(5u);
            var a2 = SaveIntegrity.DeriveKey(5u);
            var b = SaveIntegrity.DeriveKey(6u);

            Assert.That(a2, Is.EqualTo(a1));
            Assert.That(b, Is.Not.EqualTo(a1));
        }

        [Test]
        public void MatchStats_ObserveUnknownEvent_IsNoOp()
        {
            var stats = new MatchStats();

            Assert.DoesNotThrow(() => stats.Observe(new MatchEvent((MatchEventType)9999, 1, true)));
            Assert.That(stats.AwayHits + stats.HomeHits, Is.EqualTo(0));
        }

        private sealed class AlwaysFoulResolver : IContactResolver
        {
            public ContactResolution Evaluate(PitchCall pitch, SwingDecision swing, int absOffsetTicks)
                => new ContactResolution(PlayOutcome.Foul);
        }
    }
}
