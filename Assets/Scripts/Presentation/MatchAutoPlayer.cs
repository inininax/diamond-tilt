using System.Collections;
using DiamondTilt.Core;
using DiamondTilt.Core.Economy;
using UnityEngine;

namespace DiamondTilt.Presentation
{
    public sealed class MatchAutoPlayer : MonoBehaviour
    {
        private const int PitchGuard = 2000;

        private IEnumerator Start()
        {
            var engine = new MatchEngine(new TimingContactModel());
            var pitcher = new SeededPitcherAI();
            var batter = CountAwareBatterAI.ForDifficulty(Difficulty.Normal);
            var rng = new Mulberry32Rng((uint)UnityEngine.Random.Range(1, int.MaxValue));

            int pitches = 0;
            while (engine.State.Phase == MatchPhase.InProgress && pitches < PitchGuard)
            {
                var pitch = pitcher.SelectPitch(engine.State, rng);
                engine.ThrowPitch(pitch, batter.DecideSwing(pitch, engine.State, rng));
                pitches++;
                yield return new WaitForSeconds(0.02f);
            }

            Debug.Log($"[DiamondTilt] Auto-match finished: {engine.State.AwayRuns}:{engine.State.HomeRuns} " +
                      $"({engine.State.Result}) in {pitches} pitches");
        }
    }
}
