namespace DiamondTilt.Core
{
    public sealed class MatchState
    {
        public const int Innings = 3;
        public const int BallsForWalk = 4;
        public const int StrikesForOut = 3;

        public int Inning { get; internal set; } = 1;
        public bool IsTop { get; internal set; } = true;
        public int Balls { get; internal set; }
        public int Strikes { get; internal set; }
        public int Outs { get; internal set; }
        public bool FirstBase { get; internal set; }
        public bool SecondBase { get; internal set; }
        public bool ThirdBase { get; internal set; }
        public int AwayRuns { get; internal set; }
        public int HomeRuns { get; internal set; }
        public MatchPhase Phase { get; internal set; } = MatchPhase.InProgress;
        public Winner Result { get; internal set; }

        internal void AddRun()
        {
            if (IsTop) AwayRuns++;
            else HomeRuns++;
        }

        internal void ResetBatterCount()
        {
            Balls = 0;
            Strikes = 0;
        }

        internal void ResetHalfInning()
        {
            Balls = 0;
            Strikes = 0;
            Outs = 0;
            FirstBase = false;
            SecondBase = false;
            ThirdBase = false;
        }
    }
}
