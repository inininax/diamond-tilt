using System.Collections.Generic;

namespace DiamondTilt.Core
{
    public enum PlayOutcome
    {
        Foul,
        Grounder,
        LineSingle,
        Single,
        Double,
        Triple,
        Homerun,
        DeepFly
    }

    public enum MatchEventType
    {
        BallCalled,
        StrikeCalled,
        BatterWalked,
        BatterStruckOut,
        BatterOut,
        RunnerOut,
        RunnerAdvanced,
        RunScored,
        HitRecorded,
        HomerunRecorded,
        HalfInningEnded,
        MatchEnded
    }

    public readonly struct MatchEvent
    {
        public readonly MatchEventType Type;
        public readonly int Inning;
        public readonly bool IsTop;
        public readonly int Runs;

        public MatchEvent(MatchEventType type, int inning, bool isTop, int runs = 0)
        {
            Type = type;
            Inning = inning;
            IsTop = isTop;
            Runs = runs;
        }
    }

    public enum MatchPhase
    {
        InProgress,
        Finished
    }

    public enum Winner
    {
        Away,
        Home,
        Draw
    }

    public static class StrikeZone
    {
        public const int MinZone = 1;
        public const int MaxZone = 9;

        private static readonly HashSet<int> StrikeZones = new HashSet<int> { 1, 3, 4, 5, 7 };

        public static bool IsValid(int zone) => zone >= MinZone && zone <= MaxZone;

        public static bool IsStrike(int zone) => IsValid(zone) && StrikeZones.Contains(zone);
    }
}
