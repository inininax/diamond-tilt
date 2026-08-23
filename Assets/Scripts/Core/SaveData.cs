namespace DiamondTilt.Core
{
    public sealed class MatchSnapshot
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
        public int Phase { get; set; }
        public int Result { get; set; }
    }

    public sealed class SaveData
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public MatchSnapshot Match { get; set; } = new MatchSnapshot();
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int DifficultyTier { get; set; }
    }

    public static class SaveClamp
    {
        public static void Clamp(MatchSnapshot m)
        {
            if (m == null) return;
            m.Inning = ClampRange(m.Inning, 1, MatchState.Innings);
            m.Balls = ClampRange(m.Balls, 0, MatchState.BallsForWalk - 1);
            m.Strikes = ClampRange(m.Strikes, 0, MatchState.StrikesForOut - 1);
            m.Outs = ClampRange(m.Outs, 0, MatchState.StrikesForOut);
            m.AwayRuns = ClampRange(m.AwayRuns, 0, 999);
            m.HomeRuns = ClampRange(m.HomeRuns, 0, 999);
            if (m.Phase != (int)MatchPhase.Finished) m.Phase = (int)MatchPhase.InProgress;
            if (m.Result < (int)Winner.Away || m.Result > (int)Winner.Draw) m.Result = (int)Winner.Away;
        }

        public static bool IsSupportedSchema(int schemaVersion)
            => schemaVersion == SaveData.CurrentSchemaVersion;

        private static int ClampRange(int v, int min, int max)
            => v < min ? min : v > max ? max : v;
    }

    public static class MatchStateIo
    {
        public static MatchSnapshot ToSnapshot(this MatchState s)
        {
            return new MatchSnapshot
            {
                Inning = s.Inning,
                IsTop = s.IsTop,
                Balls = s.Balls,
                Strikes = s.Strikes,
                Outs = s.Outs,
                FirstBase = s.FirstBase,
                SecondBase = s.SecondBase,
                ThirdBase = s.ThirdBase,
                AwayRuns = s.AwayRuns,
                HomeRuns = s.HomeRuns,
                Phase = (int)s.Phase,
                Result = (int)s.Result,
            };
        }

        public static void Restore(this MatchState state, MatchSnapshot snapshot)
        {
            if (state == null) throw new System.ArgumentNullException(nameof(state));
            if (snapshot == null) throw new System.ArgumentNullException(nameof(snapshot));

            SaveClamp.Clamp(snapshot);
            state.Inning = snapshot.Inning;
            state.IsTop = snapshot.IsTop;
            state.Balls = snapshot.Balls;
            state.Strikes = snapshot.Strikes;
            state.Outs = snapshot.Outs;
            state.FirstBase = snapshot.FirstBase;
            state.SecondBase = snapshot.SecondBase;
            state.ThirdBase = snapshot.ThirdBase;
            state.AwayRuns = snapshot.AwayRuns;
            state.HomeRuns = snapshot.HomeRuns;
            state.Phase = (MatchPhase)snapshot.Phase;
            state.Result = (Winner)snapshot.Result;
        }
    }
}
