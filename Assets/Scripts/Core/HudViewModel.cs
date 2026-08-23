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

        public string InningLabel() => InningLabel(StringTable.Default);
        public string InningLabel(StringTable table)
            => IsTop
                ? $"{Inning}{table.Get("hud.inning.top.suffix")}"
                : $"{Inning}{table.Get("hud.inning.bottom.suffix")}";
        public string CountLabel => $"{Balls}-{Strikes}";
        public string ScoreLabel => $"{AwayRuns} : {HomeRuns}";
        public string ResultLabel(StringTable table)
            => table.Get(Phase != MatchPhase.Finished ? "hud.inning.top.suffix"
                : HomeRuns > AwayRuns ? "hud.result.win"
                : HomeRuns < AwayRuns ? "hud.result.lose"
                : "hud.result.draw");
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
