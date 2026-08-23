using System.Collections.Generic;
using System;
using DiamondTilt.Core;
using DiamondTilt.Core.Economy;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class BaseRunnerTests
    {
        [Test]
        public void Single_RunnerFromFirst_TakesSecond_BatterHoldsFirst()
        {
            var engine = MatchTestHarness.Engine(PlayOutcome.Single);
            MatchTestHarness.PlaceRunners(engine, first: true, second: false, third: false);
            MatchTestHarness.ContactPitch(engine);

            Assert.That(engine.State.FirstBase, Is.True);
            Assert.That(engine.State.SecondBase, Is.True);
            Assert.That(engine.State.ThirdBase, Is.False);
            Assert.That(engine.State.AwayRuns, Is.EqualTo(0));
        }

        [Test]
        public void Double_RunnerFromSecond_Scores()
        {
            var engine = MatchTestHarness.Engine(PlayOutcome.Double);
            MatchTestHarness.PlaceRunners(engine, first: false, second: true, third: false);
            MatchTestHarness.ContactPitch(engine);

            Assert.That(engine.State.AwayRuns, Is.EqualTo(1));
            Assert.That(engine.State.SecondBase, Is.True);
            Assert.That(engine.State.ThirdBase, Is.False);
        }

        [Test]
        public void Triple_WithBasesLoaded_ScoresThree_BatterOnThird()
        {
            var engine = MatchTestHarness.Engine(PlayOutcome.Triple);
            MatchTestHarness.PlaceRunners(engine, true, true, true);
            MatchTestHarness.ContactPitch(engine);

            Assert.That(engine.State.AwayRuns, Is.EqualTo(3));
            Assert.That(engine.State.FirstBase, Is.False);
            Assert.That(engine.State.SecondBase, Is.False);
            Assert.That(engine.State.ThirdBase, Is.True);
        }

        [Test]
        public void Homerun_GrandSlam_ScoresFour_ClearsBases()
        {
            var engine = MatchTestHarness.Engine(PlayOutcome.Homerun);
            MatchTestHarness.PlaceRunners(engine, true, true, true);
            MatchTestHarness.ContactPitch(engine);

            Assert.That(engine.State.AwayRuns, Is.EqualTo(4));
            Assert.That(engine.State.FirstBase, Is.False);
            Assert.That(engine.State.SecondBase, Is.False);
            Assert.That(engine.State.ThirdBase, Is.False);
            Assert.That(engine.State.Balls + engine.State.Strikes, Is.EqualTo(0));
        }

        [Test]
        public void Grounder_WithRunnerOnFirst_LessThanTwoOuts_IsDoublePlay()
        {
            var engine = MatchTestHarness.Engine(PlayOutcome.Grounder);
            MatchTestHarness.PlaceRunners(engine, first: true, second: false, third: false);
            MatchTestHarness.ContactPitch(engine);

            Assert.That(engine.State.Outs, Is.EqualTo(2));
            Assert.That(engine.State.FirstBase, Is.False);
            Assert.That(engine.DrainEvents(), Has.Some.Matches<MatchEvent>(e => e.Type == MatchEventType.RunnerOut));
        }

        [Test]
        public void Grounder_WithTwoOuts_NoDoublePlay_InningEndsWithoutAdvance()
        {
            var engine = MatchTestHarness.Engine(PlayOutcome.Grounder);
            MatchTestHarness.PlaceRunners(engine, first: true, second: false, third: false);
            engine.State.Outs = 2;
            MatchTestHarness.ContactPitch(engine);

            var events = engine.DrainEvents();
            Assert.That(events, Has.Some.Matches<MatchEvent>(e => e.Type == MatchEventType.HalfInningEnded));
            Assert.That(events, Has.None.Matches<MatchEvent>(e => e.Type == MatchEventType.RunnerOut));
            Assert.That(engine.State.AwayRuns, Is.EqualTo(0));
        }

        [Test]
        public void Grounder_RunnerOnThird_LessThanTwoOuts_RunScores()
        {
            var engine = MatchTestHarness.Engine(PlayOutcome.Grounder);
            MatchTestHarness.PlaceRunners(engine, false, false, true);
            MatchTestHarness.ContactPitch(engine);

            Assert.That(engine.State.Outs, Is.EqualTo(1));
            Assert.That(engine.State.AwayRuns, Is.EqualTo(1));
            Assert.That(engine.State.ThirdBase, Is.False);
        }

        [Test]
        public void DeepFly_TagUpFromThird_Scores()
        {
            var engine = MatchTestHarness.Engine(PlayOutcome.DeepFly);
            MatchTestHarness.PlaceRunners(engine, false, false, true);
            MatchTestHarness.ContactPitch(engine);

            Assert.That(engine.State.Outs, Is.EqualTo(1));
            Assert.That(engine.State.AwayRuns, Is.EqualTo(1));
            Assert.That(engine.State.ThirdBase, Is.False);
        }

        [Test]
        public void DeepFly_AsThirdOut_RunDoesNotCount_HalfEnds()
        {
            var engine = MatchTestHarness.Engine(PlayOutcome.DeepFly);
            MatchTestHarness.PlaceRunners(engine, false, false, true);
            engine.State.Outs = 2;
            MatchTestHarness.ContactPitch(engine);

            Assert.That(engine.State.AwayRuns, Is.EqualTo(0));
            Assert.That(engine.DrainEvents(), Has.Some.Matches<MatchEvent>(e => e.Type == MatchEventType.HalfInningEnded));
        }

        [Test]
        public void AdvanceAllOnHit_BasesOutsideOneToFour_Throws_StateUntouched()
        {
            var engine = MatchTestHarness.Engine();
            MatchTestHarness.PlaceRunners(engine, true, true, true);

            foreach (var bases in new[] { 0, -1, 5, int.MinValue })
            {
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    BaseRunnerEngine.AdvanceAllOnHit(engine.State, new List<MatchEvent>(), bases));
                Assert.That(engine.State.FirstBase, Is.True);
                Assert.That(engine.State.SecondBase, Is.True);
                Assert.That(engine.State.ThirdBase, Is.True);
                Assert.That(engine.State.AwayRuns, Is.EqualTo(0));
            }
        }
    }
}
