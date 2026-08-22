using System.Collections.Generic;

namespace DiamondTilt.Core
{
    public static class BaseRunnerEngine
    {
        public static void ForceAdvanceForWalk(MatchState state, List<MatchEvent> events)
        {
            bool f = state.FirstBase, s = state.SecondBase, t = state.ThirdBase;

            if (f && s && t)
            {
                ScoreRun(state, events);
            }
            else if (s && f)
            {
                state.ThirdBase = true;
                events.Add(new MatchEvent(MatchEventType.RunnerAdvanced, state.Inning, state.IsTop));
            }
            else if (f)
            {
                state.SecondBase = true;
                events.Add(new MatchEvent(MatchEventType.RunnerAdvanced, state.Inning, state.IsTop));
            }

            state.FirstBase = true;
        }

        public static void AdvanceAllOnHit(MatchState state, List<MatchEvent> events, int bases)
        {
            if (bases < 1 || bases > 4) throw new System.ArgumentOutOfRangeException(nameof(bases));

            bool n1 = false, n2 = false, n3 = false;
            int runs = 0;

            void Move(int fromBase)
            {
                int dest = fromBase + bases;
                if (dest >= 4) runs++;
                else if (dest == 1) n1 = true;
                else if (dest == 2) n2 = true;
                else n3 = true;
            }

            if (state.FirstBase) Move(1);
            if (state.SecondBase) Move(2);
            if (state.ThirdBase) Move(3);
            Move(0);

            state.FirstBase = n1;
            state.SecondBase = n2;
            state.ThirdBase = n3;
            for (int i = 0; i < runs; i++)
            {
                ScoreRun(state, events);
            }
        }

        public static void ScoreRun(MatchState state, List<MatchEvent> events)
        {
            state.AddRun();
            events.Add(new MatchEvent(MatchEventType.RunScored, state.Inning, state.IsTop, 1));
        }
    }
}
