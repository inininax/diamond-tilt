using System;
using System.Collections.Generic;
using System.Linq;
using DiamondTilt.Core;
using DiamondTilt.Core.Economy;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class DeterminismPropertyTests
    {
        private static List<(int, int, bool)> PlayAndRecordStream(uint seed)
        {
            var engine = new MatchEngine(new TimingContactModel());
            AutoMatch.PlaySelfContained(engine, Difficulty.Normal, seed);
            var stream = new List<(int, int, bool)>();
            foreach (var e in engine.DrainEvents())
            {
                stream.Add(((int)e.Type, e.Inning, e.IsTop));
            }
            return stream;
        }

        [Test]
        public void MultiSeed_EventStreams_IdenticalForSameSeed()
        {
            for (uint seed = 1; seed <= 10; seed++)
            {
                var a = PlayAndRecordStream(seed);
                var b = PlayAndRecordStream(seed);

                Assert.That(b, Is.EqualTo(a), $"seed {seed}");
            }
        }

        [Test]
        public void MultiSeed_DifferentSeeds_Diverge()
        {
            var reference = PlayAndRecordStream(1u);
            int diverged = 0;
            for (uint seed = 2; seed <= 6; seed++)
            {
                if (!PlayAndRecordStream(seed).SequenceEqual(reference)) diverged++;
            }

            Assert.That(diverged, Is.GreaterThan(0));
        }

        [Test]
        public void Property_OutsNeverExceedThree_AtAnyPoint()
        {
            for (uint seed = 300; seed < 310; seed++)
            {
                var engine = new MatchEngine(new TimingContactModel());
                var rng = new Mulberry32Rng(seed);
                int guard = AutoMatch.DefaultPitchGuard;
                while (engine.State.Phase == MatchPhase.InProgress && guard-- > 0)
                {
                    Assert.That(engine.State.Outs, Is.InRange(0, 3));

                    var pitch = new SeededPitcherAI().SelectPitch(engine.State, rng);
                    var swing = CountAwareBatterAI.ForDifficulty(Difficulty.Normal).DecideSwing(pitch, engine.State, rng);
                    engine.ThrowPitch(pitch, swing);
                    Assert.That(engine.State.Outs, Is.InRange(0, 3));
                }
            }
        }

        [Test]
        public void Property_FinalScore_EqualsSumOfRunScoredEvents()
        {
            for (uint seed = 400; seed < 410; seed++)
            {
                var engine = new MatchEngine(new TimingContactModel());
                var rng = new Mulberry32Rng(seed);
                var pitcher = new SeededPitcherAI();
                var batter = CountAwareBatterAI.ForDifficulty(Difficulty.Normal);
                int guard = AutoMatch.DefaultPitchGuard;
                while (engine.State.Phase == MatchPhase.InProgress && guard-- > 0)
                {
                    var pitch = pitcher.SelectPitch(engine.State, rng);
                    engine.ThrowPitch(pitch, batter.DecideSwing(pitch, engine.State, rng));
                }

                int awayFromEvents = 0, homeFromEvents = 0;
                foreach (var e in engine.DrainEvents())
                {
                    if (e.Type != MatchEventType.RunScored) continue;
                    if (e.IsTop) awayFromEvents++; else homeFromEvents++;
                }

                Assert.That(awayFromEvents, Is.EqualTo(engine.State.AwayRuns), $"seed {seed}");
                Assert.That(homeFromEvents, Is.EqualTo(engine.State.HomeRuns), $"seed {seed}");
            }
        }

        [Test]
        public void RngDrawAccounting_WeightedModel_ExactlyOneDrawPerContact()
        {
            var counting = new CountingRng(9u);
            var model = new WeightedContactResolver(new WeightedContactModel(counting));
            int before = counting.NextIntCalls;

            model.Evaluate(new PitchCall(4), SwingDecision.Swing(0), 0, 1);

            Assert.That(counting.NextIntCalls - before, Is.EqualTo(1));
        }

        [Test]
        public void PerfSmoke_TenAutoMatches_UnderFiveSeconds()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (uint seed = 500; seed < 510; seed++)
            {
                var engine = new MatchEngine(new TimingContactModel());
                AutoMatch.PlaySelfContained(engine, Difficulty.Normal, seed);
            }
            sw.Stop();

            Assert.That(sw.Elapsed.TotalSeconds, Is.LessThan(5.0));
        }

        private sealed class CountingRng : IRngService
        {
            public readonly Mulberry32Rng Inner;
            public int NextIntCalls;

            public CountingRng(uint seed) => Inner = new Mulberry32Rng(seed);

            public double NextDouble() => Inner.NextDouble();

            public int NextInt(int maxExclusive)
            {
                NextIntCalls++;
                return Inner.NextInt(maxExclusive);
            }
        }
    }
}
