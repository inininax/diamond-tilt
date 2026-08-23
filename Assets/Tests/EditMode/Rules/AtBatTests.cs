using System;
using DiamondTilt.Core;
using DiamondTilt.Core.Economy;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class AtBatTests
    {
        [Test]
        public void BallOutsideZone_CountsBall_AndEmitsEvent()
        {
            var engine = MatchTestHarness.Engine();
            MatchTestHarness.TakeBall(engine);

            Assert.That(engine.State.Balls, Is.EqualTo(1));
            var events = engine.DrainEvents();
            Assert.That(events, Has.Some.Matches<MatchEvent>(e => e.Type == MatchEventType.BallCalled));
        }

        [Test]
        public void FourthBall_WalksBatter_ToFirst_CountResets()
        {
            var engine = MatchTestHarness.Engine();
            for (int i = 0; i < 4; i++) MatchTestHarness.TakeBall(engine);

            Assert.That(engine.State.FirstBase, Is.True);
            Assert.That(engine.State.Balls, Is.EqualTo(0));
            Assert.That(engine.State.Strikes, Is.EqualTo(0));
            Assert.That(engine.DrainEvents(), Has.Some.Matches<MatchEvent>(e => e.Type == MatchEventType.BatterWalked));
        }

        [Test]
        public void Walk_WithFirstAndThird_LoadsBases_NoRunScores()
        {
            var engine = MatchTestHarness.Engine();
            MatchTestHarness.PlaceRunners(engine, first: true, second: false, third: true);
            for (int i = 0; i < 4; i++) MatchTestHarness.TakeBall(engine);

            Assert.That(engine.State.AwayRuns, Is.EqualTo(0));
            Assert.That(engine.State.FirstBase, Is.True);
            Assert.That(engine.State.SecondBase, Is.True);
            Assert.That(engine.State.ThirdBase, Is.True);
        }

        [Test]
        public void Walk_WithBasesLoaded_ForcesRunIn()
        {
            var engine = MatchTestHarness.Engine();
            MatchTestHarness.PlaceRunners(engine, first: true, second: true, third: true);
            for (int i = 0; i < 4; i++) MatchTestHarness.TakeBall(engine);

            Assert.That(engine.State.AwayRuns, Is.EqualTo(1));
            Assert.That(engine.State.FirstBase, Is.True);
            Assert.That(engine.State.SecondBase, Is.True);
            Assert.That(engine.State.ThirdBase, Is.True);
        }

        [Test]
        public void StrikeInZone_CountsStrike()
        {
            var engine = MatchTestHarness.Engine();
            MatchTestHarness.TakeStrike(engine);

            Assert.That(engine.State.Strikes, Is.EqualTo(1));
        }

        [Test]
        public void ThirdStrike_StrikesOutBatter()
        {
            var engine = MatchTestHarness.Engine();
            MatchTestHarness.StrikeOutBatter(engine);

            Assert.That(engine.State.Outs, Is.EqualTo(1));
            Assert.That(engine.DrainEvents(), Has.Some.Matches<MatchEvent>(e => e.Type == MatchEventType.BatterStruckOut));
        }

        [Test]
        public void Foul_WithTwoStrikes_DoesNotAddThird()
        {
            var engine = MatchTestHarness.Engine(PlayOutcome.Foul);
            MatchTestHarness.TakeStrike(engine);
            MatchTestHarness.TakeStrike(engine);
            engine.ThrowPitch(new PitchCall(MatchTestHarness.CenterZone), SwingDecision.Swing(2));

            Assert.That(engine.State.Strikes, Is.EqualTo(2));
            Assert.That(engine.State.Outs, Is.EqualTo(0));
        }

        [Test]
        public void SwingFarBeyondTiming_IsSwingingMiss_Strike()
        {
            var engine = MatchTestHarness.Engine();
            engine.ThrowPitch(new PitchCall(MatchTestHarness.CenterZone), SwingDecision.Swing(6));

            Assert.That(engine.State.Strikes, Is.EqualTo(1));
        }

        [Test]
        public void Swing_MinIntOffset_TreatedAsSwingingMiss_NoThrow()
        {
            var engine = MatchTestHarness.Engine();

            Assert.DoesNotThrow(() =>
                engine.ThrowPitch(new PitchCall(MatchTestHarness.CenterZone), SwingDecision.Swing(int.MinValue)));
            Assert.That(engine.State.Strikes, Is.EqualTo(1));
        }

        [Test]
        public void ThrowPitch_ZoneOutsideOneToNine_Throws_StateUntouched()
        {
            var engine = MatchTestHarness.Engine();

            foreach (var zone in new[] { 0, 10, -3, int.MinValue })
            {
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    engine.ThrowPitch(new PitchCall(zone), SwingDecision.Take()));
            }
            Assert.That(engine.State.Balls, Is.EqualTo(0));
        }

        [Test]
        public void StrikeZone_IsValid_AcceptsOnlyOneThroughNine()
        {
            Assert.That(StrikeZone.IsValid(1), Is.True);
            Assert.That(StrikeZone.IsValid(9), Is.True);
            Assert.That(StrikeZone.IsValid(0), Is.False);
            Assert.That(StrikeZone.IsValid(10), Is.False);
            Assert.That(StrikeZone.IsValid(-1), Is.False);
        }
    }
}
