using System.Collections.Generic;
using DiamondTilt.Core;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class AiPlayersTests
    {
        private sealed class CountingRng : IRngService
        {
            public readonly Mulberry32Rng Inner;
            public int NextIntCalls;
            public int NextDoubleCalls;
            public readonly List<int> Ints = new List<int>();

            public CountingRng(uint seed) => Inner = new Mulberry32Rng(seed);

            public double NextDouble()
            {
                NextDoubleCalls++;
                return Inner.NextDouble();
            }

            public int NextInt(int maxExclusive)
            {
                NextIntCalls++;
                int v = Inner.NextInt(maxExclusive);
                Ints.Add(v);
                return v;
            }
        }

        [Test]
        public void PitcherAI_ThreeBalls_AlwaysThrowsStrikeZone()
        {
            var rng = new Mulberry32Rng(7u);
            var pitcher = new SeededPitcherAI();
            var state = new MatchState { Balls = 3 };

            for (int i = 0; i < 200; i++)
            {
                var pitch = pitcher.SelectPitch(state, rng);
                Assert.That(StrikeZone.IsStrike(pitch.Zone), Is.True, $"pitch {i} zone {pitch.Zone}");
            }
        }

        [Test]
        public void PitcherAI_EarlyCount_MixesInBalls()
        {
            var rng = new Mulberry32Rng(11u);
            var pitcher = new SeededPitcherAI();
            var state = new MatchState();

            bool sawBall = false, sawStrike = false;
            for (int i = 0; i < 100; i++)
            {
                var zone = pitcher.SelectPitch(state, rng).Zone;
                if (StrikeZone.IsStrike(zone)) sawStrike = true;
                else sawBall = true;
            }

            Assert.That(sawStrike && sawBall, Is.True);
        }

        [Test]
        public void BatterAI_TwoStrikes_SwingsMoreThanAheadInCount()
        {
            const uint seed = 42u;
            int swingsAhead = CountSwings(seed, balls: 0, strikes: 0, samples: 400);
            int swingsTwoStrikes = CountSwings(seed, balls: 0, strikes: 2, samples: 400);

            Assert.That(swingsTwoStrikes, Is.GreaterThan(swingsAhead));
        }

        [Test]
        public void Difficulty_ScalesNoiseSigma()
        {
            Assert.That(CountAwareBatterAI.ForDifficulty(Difficulty.Easy).NoiseSigmaTicks,
                Is.GreaterThan(CountAwareBatterAI.ForDifficulty(Difficulty.Hard).NoiseSigmaTicks));
        }

        [Test]
        public void AutoMatch_SelfContained_AllSeedsFinish()
        {
            for (uint seed = 1; seed <= 20; seed++)
            {
                var engine = new MatchEngine(new TimingContactModel());
                AutoMatch.PlaySelfContained(engine, Difficulty.Normal, seed);

                Assert.That(engine.State.Phase, Is.EqualTo(MatchPhase.Finished), $"seed {seed}");
            }
        }

        [Test]
        public void AutoMatch_Balance_TotalRunsInSaneBand()
        {
            double totalRuns = 0;
            const int games = 20;
            for (uint seed = 100; seed < 100 + games; seed++)
            {
                var engine = new MatchEngine(new TimingContactModel());
                AutoMatch.PlaySelfContained(engine, Difficulty.Normal, seed);
                totalRuns += engine.State.AwayRuns + engine.State.HomeRuns;
            }

            double mean = totalRuns / games;
            Assert.That(mean, Is.GreaterThanOrEqualTo(0.5));
            Assert.That(mean, Is.LessThanOrEqualTo(30.0));
        }

        [Test]
        public void AutoMatch_WalkRate_Bounded()
        {
            int walks = 0, pitches = 0;
            for (uint seed = 200; seed < 210; seed++)
            {
                var engine = new MatchEngine(new TimingContactModel());
                AutoMatch.PlaySelfContained(engine, Difficulty.Normal, seed);
                foreach (var e in engine.DrainEvents())
                {
                    pitches++;
                    if (e.Type == MatchEventType.BatterWalked) walks++;
                }
            }

            Assert.That(walks / (double)pitches, Is.LessThan(0.35));
        }

        [Test]
        public void AutoMatch_SameSeed_ReplayIdenticalScoresAndEventCounts()
        {
            var a = PlayAndRecord(777u);
            var b = PlayAndRecord(777u);

            Assert.That(b.Score, Is.EqualTo(a.Score));
            Assert.That(b.EventCount, Is.EqualTo(a.EventCount));
        }

        private static (int Score, int EventCount) PlayAndRecord(uint seed)
        {
            var engine = new MatchEngine(new TimingContactModel());
            AutoMatch.PlaySelfContained(engine, Difficulty.Normal, seed);
            return (engine.State.AwayRuns * 10 + engine.State.HomeRuns, engine.DrainEvents().Count);
        }

        private static int CountSwings(uint seed, int balls, int strikes, int samples)
        {
            var rng = new Mulberry32Rng(seed);
            var batter = CountAwareBatterAI.ForDifficulty(Difficulty.Normal);
            var state = new MatchState { Balls = balls, Strikes = strikes };
            int swings = 0;
            for (int i = 0; i < samples; i++)
            {
                if (batter.DecideSwing(new PitchCall(4), state, rng).Took) swings++;
            }
            return swings;
        }
    }
}
