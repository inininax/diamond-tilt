using System;

namespace DiamondTilt.Core
{
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
    }

    public interface IPitchStrategy
    {
        PitchCall SelectPitch(MatchState state, IRngService rng);
    }

    public sealed class SeededPitcherAI : IPitchStrategy
    {
        private static readonly int[] StrikeZonesList = { 1, 3, 4, 5, 7 };

        public PitchCall SelectPitch(MatchState state, IRngService rng)
        {
            bool mustThrowStrike = state.Balls >= MatchState.BallsForWalk - 1;
            int zone = mustThrowStrike
                ? StrikeZonesList[rng.NextInt(StrikeZonesList.Length)]
                : rng.NextInt(StrikeZone.MaxZone) + StrikeZone.MinZone;
            int speedTier = rng.NextInt(PitchCall.MaxSpeedTier + 1);
            return new PitchCall(zone, speedTier);
        }
    }

    public interface ISwingStrategy
    {
        SwingDecision DecideSwing(PitchCall pitch, MatchState state, IRngService rng);
    }

    public sealed class CountAwareBatterAI : ISwingStrategy
    {
        private readonly double _noiseSigmaTicks;
        private readonly double _aggressionBase;

        public CountAwareBatterAI(double noiseSigmaTicks, double aggressionBase)
        {
            if (noiseSigmaTicks <= 0 || noiseSigmaTicks > 6) throw new ArgumentOutOfRangeException(nameof(noiseSigmaTicks));
            if (aggressionBase <= 0 || aggressionBase > 1) throw new ArgumentOutOfRangeException(nameof(aggressionBase));

            _noiseSigmaTicks = noiseSigmaTicks;
            _aggressionBase = aggressionBase;
        }

        public double NoiseSigmaTicks => _noiseSigmaTicks;

        public SwingDecision DecideSwing(PitchCall pitch, MatchState state, IRngService rng)
        {
            double aggression = _aggressionBase;
            if (state.Strikes >= MatchState.StrikesForOut - 1) aggression += 0.35;
            if (state.Balls >= MatchState.BallsForWalk - 1) aggression -= 0.30;
            if (aggression < 0.05) aggression = 0.05;
            if (aggression > 1.0) aggression = 1.0;

            if (rng.NextDouble() >= aggression) return SwingDecision.Take();

            return SwingDecision.Swing(GaussianTicks(rng));
        }

        private int GaussianTicks(IRngService rng)
        {
            double u = 0;
            for (int i = 0; i < 4; i++) u += rng.NextDouble();
            double ticks = (u - 2.0) * _noiseSigmaTicks;
            int rounded = (int)Math.Round(ticks, MidpointRounding.AwayFromZero);
            return Math.Max(-6, Math.Min(6, rounded));
        }

        public static CountAwareBatterAI ForDifficulty(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy: return new CountAwareBatterAI(1.6, 0.55);
                case Difficulty.Hard: return new CountAwareBatterAI(0.7, 0.75);
                default: return new CountAwareBatterAI(1.1, 0.65);
            }
        }
    }
}
