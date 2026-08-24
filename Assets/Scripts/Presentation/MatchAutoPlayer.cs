using System.Collections;
using DiamondTilt.Core;
using DiamondTilt.Core.Economy;
using UnityEngine;

namespace DiamondTilt.Presentation
{
    public sealed class MatchAutoPlayer : MonoBehaviour
    {
        private const int PitchGuard = 2000;

        public GameRunner Runner;

        private IEnumerator Start()
        {
            var runner = Runner != null ? Runner : FindFirstObjectByType<GameRunner>();
            if (runner == null)
            {
                Debug.LogError("[DiamondTilt] MatchAutoPlayer requires a GameRunner (Boot scene).");
                yield break;
            }

            uint seed = (uint)UnityEngine.Random.Range(1, int.MaxValue);
            var summary = RunFullMatch(runner.Services, Difficulty.Normal, seed);

            Debug.Log($"[DiamondTilt] Auto-match finished: {summary.AwayRuns}:{summary.HomeRuns} " +
                      $"({summary.Result}) in {summary.Pitches} pitches · " +
                      $"coins {summary.CoinsBefore}->{summary.CoinsAfter} · season xp +{summary.XpGained}");
        }

        public static MatchSummary RunFullMatch(GameServices services, Difficulty difficulty, uint seed)
        {
            var engine = new MatchEngine(new TimingContactModel());
            var pitcher = new SeededPitcherAI();
            var batter = CountAwareBatterAI.ForDifficulty(difficulty);
            var rng = new Mulberry32Rng(seed);

            long coinsBefore = services.Wallet.Coins;
            int pitches = 0;
            while (engine.State.Phase == MatchPhase.InProgress && pitches < PitchGuard)
            {
                var pitch = pitcher.SelectPitch(engine.State, rng);
                engine.ThrowPitch(pitch, batter.DecideSwing(pitch, engine.State, rng));
                pitches++;
            }

            var stats = new MatchStats();
            foreach (var e in engine.DrainEvents())
            {
                stats.Observe(e);
            }

            int xp = services.Rewards.ApplyPostMatch(engine.State.Result, stats);

            return new MatchSummary
            {
                Result = engine.State.Result,
                AwayRuns = engine.State.AwayRuns,
                HomeRuns = engine.State.HomeRuns,
                Pitches = pitches,
                CoinsBefore = coinsBefore,
                CoinsAfter = services.Wallet.Coins,
                XpGained = xp,
            };
        }
    }

    public sealed class MatchSummary
    {
        public Winner Result { get; set; }
        public int AwayRuns { get; set; }
        public int HomeRuns { get; set; }
        public int Pitches { get; set; }
        public long CoinsBefore { get; set; }
        public long CoinsAfter { get; set; }
        public int XpGained { get; set; }

        public bool EconomyProgressed => CoinsAfter > CoinsBefore || XpGained > 0;
    }
}
