using System;

namespace DiamondTilt.Core
{
    public sealed class MatchStats
    {
        public int AwayHits { get; private set; }
        public int HomeHits { get; private set; }
        public int AwayHomeruns { get; private set; }
        public int HomeHomeruns { get; private set; }
        public int AwayStrikeouts { get; private set; }
        public int HomeStrikeouts { get; private set; }

        public void Observe(MatchEvent e)
        {
            switch (e.Type)
            {
                case MatchEventType.HitRecorded:
                    if (e.IsTop) AwayHits++; else HomeHits++;
                    break;
                case MatchEventType.HomerunRecorded:
                    if (e.IsTop) { AwayHits++; AwayHomeruns++; }
                    else { HomeHits++; HomeHomeruns++; }
                    break;
                case MatchEventType.BatterStruckOut:
                    if (e.IsTop) AwayStrikeouts++; else HomeStrikeouts++;
                    break;
            }
        }
    }

    public sealed class StreakTracker
    {
        public int CurrentWinStreak { get; private set; }
        public int BestWinStreak { get; private set; }
        public int Wins { get; private set; }
        public int Losses { get; private set; }

        public void RecordPlayerResult(Winner result)
        {
            switch (result)
            {
                case Winner.Home:
                    Wins++;
                    CurrentWinStreak++;
                    if (CurrentWinStreak > BestWinStreak) BestWinStreak = CurrentWinStreak;
                    break;
                case Winner.Away:
                    Losses++;
                    CurrentWinStreak = 0;
                    break;
            }
        }
    }

    public sealed class GameSettings
    {
        public const int MinDifficulty = (int)Difficulty.Easy;
        public const int MaxDifficulty = (int)Difficulty.Hard;

        public int DifficultyTier { get; set; } = (int)Difficulty.Normal;
        public bool SoundEnabled { get; set; } = true;

        public static GameSettings Clamp(GameSettings s)
        {
            if (s == null) return new GameSettings();
            s.DifficultyTier = s.DifficultyTier < MinDifficulty ? MinDifficulty
                : s.DifficultyTier > MaxDifficulty ? MaxDifficulty : s.DifficultyTier;
            return s;
        }
    }
}
