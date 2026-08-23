using DiamondTilt.Core;

namespace DiamondTilt.Core
{
    public sealed class HudSnapshot
    {
        public int Inning { get; set; }
        public bool IsTop { get; set; }
        public int Balls { get; set; }
        public int Strikes { get; set; }
        public int Outs { get; set; }
        public bool FirstBase { get; set; }
        public bool SecondBase { get; set; }
        public bool ThirdBase { get; set; }
        public int AwayRuns { get; set; }
        public int HomeRuns { get; set; }
        public MatchPhase Phase { get; set; }

        public string InningLabel => IsTop ? $"{Inning}초" : $"{Inning}말";
        public string CountLabel => $"{Balls}-{Strikes}";
        public string ScoreLabel => $"{AwayRuns} : {HomeRuns}";
        public int BaseRunnerCount => (FirstBase ? 1 : 0) + (SecondBase ? 1 : 0) + (ThirdBase ? 1 : 0);
    }

    public static class HudMapper
    {
        private static readonly HudSnapshot Blank = new HudSnapshot
        {
            Inning = 1, IsTop = true, Phase = MatchPhase.InProgress,
        };

        public static HudSnapshot From(MatchState state)
        {
            if (state == null) return Blank;

            return new HudSnapshot
            {
                Inning = state.Inning,
                IsTop = state.IsTop,
                Balls = state.Balls,
                Strikes = state.Strikes,
                Outs = state.Outs,
                FirstBase = state.FirstBase,
                SecondBase = state.SecondBase,
                ThirdBase = state.ThirdBase,
                AwayRuns = state.AwayRuns,
                HomeRuns = state.HomeRuns,
                Phase = state.Phase,
            };
        }
    }
}
