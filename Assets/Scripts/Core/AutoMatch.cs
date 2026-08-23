using System;

namespace DiamondTilt.Core
{
    public static class AutoMatch
    {
        public const int DefaultPitchGuard = 2000;

        public static bool Play(
            MatchEngine engine,
            IPitchStrategy awayBatterFacingPitcher,
            ISwingStrategy awayBatter,
            IPitchStrategy homePitcher,
            ISwingStrategy homeBatter,
            IRngService rng)
        {
            if (engine == null) throw new ArgumentNullException(nameof(engine));

            int guard = DefaultPitchGuard;
            while (engine.State.Phase == MatchPhase.InProgress && guard-- > 0)
            {
                bool topHalf = engine.State.IsTop;
                IPitchStrategy pitcher = topHalf ? homePitcher : awayBatterFacingPitcher;
                ISwingStrategy batter = topHalf ? awayBatter : homeBatter;

                PitchCall pitch = pitcher.SelectPitch(engine.State, rng);
                SwingDecision swing = batter.DecideSwing(pitch, engine.State, rng);
                engine.ThrowPitch(pitch, swing);
            }

            return engine.State.Phase == MatchPhase.Finished;
        }

        public static bool PlaySelfContained(MatchEngine engine, Difficulty difficulty, uint seed)
        {
            var rng = new Mulberry32Rng(seed);
            var pitcher = new SeededPitcherAI();
            var batter = CountAwareBatterAI.ForDifficulty(difficulty);
            return Play(engine, pitcher, batter, pitcher, batter, rng);
        }
    }
}
