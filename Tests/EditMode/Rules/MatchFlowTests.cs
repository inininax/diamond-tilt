using System;
using DiamondTilt.Core;
using DiamondTilt.Core.Economy;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class MatchFlowTests
    {
        [Test]
        public void ThreeOuts_EndHalfInning_AndFlipSide()
        {
            var engine = MatchTestHarness.Engine();
            MatchTestHarness.StrikeOutBatter(engine);
            MatchTestHarness.StrikeOutBatter(engine);
            MatchTestHarness.StrikeOutBatter(engine);

            Assert.That(engine.State.IsTop, Is.False);
            Assert.That(engine.State.Inning, Is.EqualTo(1));
            Assert.That(engine.State.Outs, Is.EqualTo(0));
        }

        [Test]
        public void SixOuts_AdvanceToSecondInning()
        {
            var engine = MatchTestHarness.Engine();
            for (int i = 0; i < 6; i++) MatchTestHarness.StrikeOutBatter(engine);

            Assert.That(engine.State.Inning, Is.EqualTo(2));
            Assert.That(engine.State.IsTop, Is.True);
        }

        [Test]
        public void HalfInningEnd_ClearsBasesAndCount()
        {
            var engine = MatchTestHarness.Engine(PlayOutcome.Single);
            MatchTestHarness.PlaceRunners(engine, true, true, false);
            MatchTestHarness.StrikeOutBatter(engine);
            MatchTestHarness.StrikeOutBatter(engine);

            MatchTestHarness.PlaceRunners(engine, true, false, false);
            MatchTestHarness.StrikeOutBatter(engine);

            Assert.That(engine.State.FirstBase, Is.False);
            Assert.That(engine.State.Balls + engine.State.Strikes + engine.State.Outs, Is.EqualTo(0));
        }

        [Test]
        public void FullMatch_AfterThreeInnings_Finishes()
        {
            var engine = MatchTestHarness.Engine();
            for (int i = 0; i < 18; i++) MatchTestHarness.StrikeOutBatter(engine);

            Assert.That(engine.State.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(engine.State.Result, Is.EqualTo(Winner.Draw));
            Assert.That(engine.DrainEvents(), Has.Some.Matches<MatchEvent>(e => e.Type == MatchEventType.MatchEnded));
        }

        [Test]
        public void FinishedMatch_IgnoresFurtherPitches()
        {
            var engine = MatchTestHarness.Engine();
            for (int i = 0; i < 18; i++) MatchTestHarness.StrikeOutBatter(engine);
            engine.DrainEvents();

            MatchTestHarness.TakeBall(engine);

            Assert.That(engine.DrainEvents(), Is.Empty);
        }

        [Test]
        public void DrainEvents_ClearsQueue()
        {
            var engine = MatchTestHarness.Engine();
            MatchTestHarness.TakeStrike(engine);

            Assert.That(engine.DrainEvents(), Is.Not.Empty);
            Assert.That(engine.DrainEvents(), Is.Empty);
        }

        [Test]
        public void SameSeed_ProducesIdenticalMatchOutcome()
        {
            int scoreA = SimulateRandomMatch(12345u);
            int scoreB = SimulateRandomMatch(12345u);

            Assert.That(scoreA, Is.EqualTo(scoreB));
        }

        [Test]
        public void RunsCarryAcrossHalfInnings_ToFinalResult()
        {
            var engine = MatchTestHarness.Engine();
            for (int i = 0; i < 3; i++)
            {
                if (!engine.State.IsTop || engine.State.Outs >= 2)
                {
                    MatchTestHarness.StrikeOutBatter(engine);
                    continue;
                }
                MatchTestHarness.PlaceRunners(engine, false, false, true);
                MatchTestHarness.ContactPitch(engine);
            }
            while (engine.State.Phase == MatchPhase.InProgress)
            {
                MatchTestHarness.StrikeOutBatter(engine);
            }

            Assert.That(engine.State.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(engine.State.AwayRuns, Is.GreaterThan(engine.State.HomeRuns));
        }

        [Test]
        public void FinalInning_TopHalfEnds_HomeAhead_SkipsBottom_AndFinishes()
        {
            var engine = MatchTestHarness.Engine();
            SetFinalInningTopHalf(engine, awayRuns: 0, homeRuns: 1);
            for (int i = 0; i < 3; i++)
            {
                MatchTestHarness.StrikeOutBatter(engine);
            }

            Assert.That(engine.State.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(engine.State.Result, Is.EqualTo(Winner.Home));
            Assert.That(engine.State.IsTop, Is.True);
            Assert.That(engine.DrainEvents(), Has.Some.Matches<MatchEvent>(e =>
                e.Type == MatchEventType.MatchEnded && e.IsTop));
        }

        [Test]
        public void FinalInning_TopHalfEnds_Tied_BottomPlayed()
        {
            var engine = MatchTestHarness.Engine();
            SetFinalInningTopHalf(engine, awayRuns: 0, homeRuns: 0);
            for (int i = 0; i < 3; i++)
            {
                MatchTestHarness.StrikeOutBatter(engine);
            }

            Assert.That(engine.State.Phase, Is.EqualTo(MatchPhase.InProgress));
            Assert.That(engine.State.IsTop, Is.False);
            Assert.That(engine.State.Inning, Is.EqualTo(3));
        }

        [Test]
        public void FinalInning_TopHalfEnds_HomeBehind_BottomPlayed()
        {
            var engine = MatchTestHarness.Engine();
            SetFinalInningTopHalf(engine, awayRuns: 2, homeRuns: 1);
            for (int i = 0; i < 3; i++)
            {
                MatchTestHarness.StrikeOutBatter(engine);
            }

            Assert.That(engine.State.Phase, Is.EqualTo(MatchPhase.InProgress));
            Assert.That(engine.State.IsTop, Is.False);
        }

        private static void SetFinalInningTopHalf(MatchEngine engine, int awayRuns, int homeRuns)
        {
            engine.State.Inning = MatchState.Innings;
            engine.State.IsTop = true;
            engine.State.AwayRuns = awayRuns;
            engine.State.HomeRuns = homeRuns;
        }

        [Test]
        public void Contact_UnknownOutcomeFromModel_ThrowsInvalidOperation_StateUntouched()
        {
            var engine = new MatchEngine(new StubContactModel((PlayOutcome)999));

            Assert.Throws<InvalidOperationException>(() => MatchTestHarness.ContactPitch(engine));
            Assert.That(engine.State.Outs + engine.State.Balls + engine.State.Strikes, Is.EqualTo(0));
        }

        [Test]
        public void PlayOutcome_ExcludesPitchCallValues()
        {
            var names = Enum.GetNames(typeof(PlayOutcome));

            Assert.That(names, Has.No.Member("CalledBall"));
            Assert.That(names, Has.No.Member("CalledStrike"));
            Assert.That(names, Has.No.Member("SwingingMiss"));
        }

        [Test]
        public void DrainEvents_PreviousReturn_ClearedByNextDrain()
        {
            var engine = MatchTestHarness.Engine();
            MatchTestHarness.TakeStrike(engine);
            var first = engine.DrainEvents();

            MatchTestHarness.TakeStrike(engine);
            var second = engine.DrainEvents();

            Assert.That(second, Is.Not.Empty);
            Assert.That(first, Is.Empty);
        }

        [Test]
        public void WeightedContactModel_RollBoundaries_MapToExpectedOutcomes()
        {
            var cases = new (int roll, PlayOutcome expected)[]
            {
                (0, PlayOutcome.Grounder), (39, PlayOutcome.Grounder),
                (40, PlayOutcome.Single), (64, PlayOutcome.Single),
                (65, PlayOutcome.DeepFly), (84, PlayOutcome.DeepFly),
                (85, PlayOutcome.Foul), (94, PlayOutcome.Foul),
                (95, PlayOutcome.Homerun), (99, PlayOutcome.Homerun),
            };

            foreach (var (roll, expected) in cases)
            {
                var model = new WeightedContactModel(new FixedRng(roll));
                Assert.That(model.Roll(), Is.EqualTo(expected), $"roll {roll}");
            }
        }

        private sealed class FixedRng : IRngService
        {
            private readonly int _value;

            public FixedRng(int value) => _value = value;

            public double NextDouble() => _value / 100.0;

            public int NextInt(int maxExclusive) => System.Math.Min(_value, maxExclusive - 1);
        }

        private static int SimulateRandomMatch(uint seed)
        {
            var engine = new MatchEngine(new WeightedContactModel(new Mulberry32Rng(seed)));
            var rng = new Mulberry32Rng(seed ^ 999u);
            int guard = 0;
            while (engine.State.Phase == MatchPhase.InProgress && guard++ < 1000)
            {
                bool took = rng.NextInt(4) > 0;
                int offset = rng.NextInt(9) - 4;
                var swing = took ? SwingDecision.Swing(offset) : SwingDecision.Take();
                engine.ThrowPitch(new PitchCall(rng.NextInt(9) + 1), swing);
            }
            return engine.State.AwayRuns + engine.State.HomeRuns * 10;
        }
    }
}
