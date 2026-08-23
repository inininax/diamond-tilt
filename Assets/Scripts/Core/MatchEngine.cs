using System;
using System.Collections.Generic;

namespace DiamondTilt.Core
{
    public sealed class MatchEngine
    {
        public const int MissOffsetThresholdTicks = 4;

        private readonly IContactResolver _contactResolver;
        private List<MatchEvent> _events = new List<MatchEvent>();
        private List<MatchEvent> _spare = new List<MatchEvent>();

        public MatchState State { get; } = new MatchState();

        public MatchEngine(IRngService rng) : this(new WeightedContactModel(rng))
        {
        }

        public MatchEngine(IContactModel contactModel) : this(new WeightedContactResolver(contactModel))
        {
        }

        public MatchEngine(IContactResolver contactResolver)
        {
            _contactResolver = contactResolver;
        }

        public IReadOnlyList<MatchEvent> DrainEvents()
        {
            var drained = _events;
            _events = _spare;
            _spare = drained;
            _events.Clear();
            return drained;
        }

        public void ThrowPitch(PitchCall pitch, SwingDecision swing)
        {
            if (!StrikeZone.IsValid(pitch.Zone)) throw new ArgumentOutOfRangeException(nameof(pitch));
            if (State.Phase == MatchPhase.Finished) return;

            if (!swing.Took)
            {
                if (StrikeZone.IsStrike(pitch.Zone)) AddStrike();
                else ResolveBall();
                return;
            }

            int offset = swing.TimingOffsetTicks;
            int abs = offset == int.MinValue ? int.MaxValue : offset < 0 ? -offset : offset;

            int missThreshold = MissOffsetThresholdTicks - SpeedWindowPenalty(pitch.SpeedTier);
            int perfectBand = pitch.SpeedTier == PitchCall.MinSpeedTier ? 1 : 0;

            if (abs >= missThreshold)
            {
                AddStrike();
                return;
            }

            ResolveContact(pitch, swing, abs, perfectBand);
        }

        private static int SpeedWindowPenalty(int speedTier) => speedTier;

        private void ResolveBall()
        {
            State.Balls++;
            if (State.Balls >= MatchState.BallsForWalk)
            {
                _events.Add(new MatchEvent(MatchEventType.BatterWalked, State.Inning, State.IsTop));
                BaseRunnerEngine.ForceAdvanceForWalk(State, _events);
                State.ResetBatterCount();
                CheckHalfInningEnd();
                CheckWalkoff();
                return;
            }
            _events.Add(new MatchEvent(MatchEventType.BallCalled, State.Inning, State.IsTop));
        }

        private void AddStrike()
        {
            State.Strikes++;
            if (State.Strikes >= MatchState.StrikesForOut)
            {
                RecordOut(MatchEventType.BatterStruckOut);
                return;
            }
            _events.Add(new MatchEvent(MatchEventType.StrikeCalled, State.Inning, State.IsTop));
        }

        private void ResolveFoul()
        {
            if (State.Strikes < MatchState.StrikesForOut - 1) State.Strikes++;
            _events.Add(new MatchEvent(MatchEventType.StrikeCalled, State.Inning, State.IsTop));
        }

        private void ResolveContact(PitchCall pitch, SwingDecision swing, int absOffsetTicks, int perfectBand)
        {
            ContactResolution resolution = _contactResolver.Evaluate(pitch, swing, absOffsetTicks, perfectBand);
            switch (resolution.Outcome)
            {
                case PlayOutcome.Foul:
                    ResolveFoul();
                    break;
                case PlayOutcome.Grounder:
                    ResolveGrounder();
                    break;
                case PlayOutcome.Homerun:
                case PlayOutcome.DeepFly when ClearsWall(resolution):
                    ResolveHit(4);
                    break;
                case PlayOutcome.DeepFly:
                    ResolveDeepFly();
                    break;
                case PlayOutcome.Single:
                case PlayOutcome.LineSingle:
                    ResolveHit(1);
                    break;
                case PlayOutcome.Double:
                    ResolveHit(2);
                    break;
                case PlayOutcome.Triple:
                    ResolveHit(3);
                    break;
                default:
                    throw new InvalidOperationException($"Unhandled contact outcome: {resolution.Outcome}");
            }
        }

        private static bool ClearsWall(ContactResolution resolution)
        {
            if (resolution.Outcome == PlayOutcome.Homerun) return true;
            return resolution.Flight.HasValue && BallFlight.ClearsWallForHomerun(resolution.Flight.Value);
        }

        private void ResolveHit(int bases)
        {
            BaseRunnerEngine.AdvanceAllOnHit(State, _events, bases);
            _events.Add(new MatchEvent(
                bases >= 4 ? MatchEventType.HomerunRecorded : MatchEventType.HitRecorded,
                State.Inning, State.IsTop));
            State.ResetBatterCount();
            CheckHalfInningEnd();
            CheckWalkoff();
        }

        private void ResolveGrounder()
        {
            if (State.FirstBase && State.Outs < 2)
            {
                RecordOut(MatchEventType.RunnerOut);
                State.FirstBase = false;
                RecordOut(MatchEventType.BatterOut);
                return;
            }

            bool runnerScoresOnContact = State.ThirdBase && State.Outs < 2;
            RecordOut(MatchEventType.BatterOut);
            if (runnerScoresOnContact && State.Outs < 3)
            {
                State.ThirdBase = false;
                BaseRunnerEngine.ScoreRun(State, _events);
            }
            CheckHalfInningEnd();
            CheckWalkoff();
        }

        private void ResolveDeepFly()
        {
            bool runnerTagsUp = State.ThirdBase && State.Outs < 2;
            RecordOut(MatchEventType.BatterOut);
            if (runnerTagsUp && State.Outs < 3)
            {
                State.ThirdBase = false;
                BaseRunnerEngine.ScoreRun(State, _events);
            }
            CheckHalfInningEnd();
            CheckWalkoff();
        }

        private void RecordOut(MatchEventType eventType)
        {
            State.Outs++;
            _events.Add(new MatchEvent(eventType, State.Inning, State.IsTop));
            State.ResetBatterCount();
            CheckHalfInningEnd();
        }

        private void CheckWalkoff()
        {
            if (State.Phase == MatchPhase.Finished) return;
            if (State.IsTop || State.Inning < MatchState.Innings) return;
            if (State.HomeRuns > State.AwayRuns) FinishMatch();
        }

        private void CheckHalfInningEnd()
        {
            if (State.Outs < 3 || State.Phase == MatchPhase.Finished) return;

            _events.Add(new MatchEvent(MatchEventType.HalfInningEnded, State.Inning, State.IsTop));

            if (!State.IsTop)
            {
                if (State.Inning >= MatchState.Innings)
                {
                    FinishMatch();
                    return;
                }
                State.Inning++;
                State.IsTop = true;
            }
            else
            {
                if (State.Inning >= MatchState.Innings && State.HomeRuns > State.AwayRuns)
                {
                    FinishMatch();
                    return;
                }
                State.IsTop = false;
            }

            State.ResetHalfInning();
        }

        private void FinishMatch()
        {
            State.Phase = MatchPhase.Finished;
            State.Result = State.AwayRuns > State.HomeRuns ? Winner.Away
                : State.HomeRuns > State.AwayRuns ? Winner.Home
                : Winner.Draw;
            _events.Add(new MatchEvent(MatchEventType.MatchEnded, State.Inning, State.IsTop));
        }
    }

    public interface IContactModel
    {
        PlayOutcome Roll();
    }

    public sealed class WeightedContactModel : IContactModel
    {
        private static readonly (PlayOutcome Outcome, int Weight)[] Table =
        {
            (PlayOutcome.Grounder, 40),
            (PlayOutcome.Single, 25),
            (PlayOutcome.DeepFly, 20),
            (PlayOutcome.Foul, 10),
            (PlayOutcome.Homerun, 5),
        };

        private static readonly int TotalWeight = BuildTotalWeight();

        private readonly IRngService _rng;

        public WeightedContactModel(IRngService rng)
        {
            _rng = rng;
        }

        public PlayOutcome Roll()
        {
            int roll = _rng.NextInt(TotalWeight);
            foreach (var entry in Table)
            {
                if (roll < entry.Weight) return entry.Outcome;
                roll -= entry.Weight;
            }
            return Table[Table.Length - 1].Outcome;
        }

        private static int BuildTotalWeight()
        {
            int total = 0;
            foreach (var entry in Table) total += entry.Weight;
            return total;
        }
    }
}
