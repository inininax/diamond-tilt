using System;
using System.Collections.Generic;
using DiamondTilt.Core.Economy;

namespace DiamondTilt.Core
{
    public enum SessionPhase
    {
        WaitingToPitch,
        BallIncoming,
        BetweenPlays,
        MatchOver,
    }

    public sealed class MatchSummaryPayload
    {
        public Winner Result { get; set; }
        public int AwayRuns { get; set; }
        public int HomeRuns { get; set; }
    }

    public sealed class MatchPlaySession
    {
        private const int BetweenPlaysTicks = 45;
        private const int SlowFlightTicks = 64;
        private const int NormalFlightTicks = 52;
        private const int FastFlightTicks = 40;

        private readonly MatchEngine _engine;
        private readonly SeededPitcherAI _cpuPitcher = new SeededPitcherAI();
        private readonly CountAwareBatterAI _cpuBatter;
        private readonly IRngService _rng;
        private readonly MatchRewardService _rewards;

        private readonly TimingContactModel _preResolver = new TimingContactModel();
        private PitchCall _incomingPitch;
        private int _resumeTick;
        private bool _rewardsApplied;

        public LaunchParams? LastContactFlight { get; private set; }
        public PlayOutcome LastOutcome { get; private set; }
        public bool LastContactWasSwing { get; private set; }
        public PitchCall IncomingPitch => _incomingPitch;

        public MatchState State => _engine.State;
        public int TicksPerSecond { get; }
        public int CurrentTick { get; private set; }
        public SessionPhase Phase { get; private set; }
        public int IncomingArrivalTick { get; private set; } = -1;
        public int FlightTicks { get; private set; }
        public bool PlayerBatting => !State.IsTop;

        public MatchPlaySession(MatchEngine engine, IRngService rng, MatchRewardService rewards = null, int ticksPerSecond = 60)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _rewards = rewards;
            TicksPerSecond = Math.Clamp(ticksPerSecond, 30, 240);
            _cpuBatter = CountAwareBatterAI.ForDifficulty(Difficulty.Normal);
            BeginNextPlay();
        }

        public IReadOnlyList<MatchEvent> DrainEvents() => _engine.DrainEvents();

        public void TickAdvance(int ticks = 1)
        {
            if (Phase == SessionPhase.MatchOver) return;

            for (int i = 0; i < ticks; i++)
            {
                CurrentTick++;

                if (Phase == SessionPhase.BallIncoming && CurrentTick > IncomingArrivalTick + TouchToIntent.MaxOffsetTicks)
                {
                    ResolveTake();
                }
                else if (Phase == SessionPhase.BetweenPlays && CurrentTick >= _resumeTick)
                {
                    BeginNextPlay();
                    if (Phase == SessionPhase.MatchOver) return;
                }
            }
        }

        public bool PlayerPitch(int zone, int speedTier)
        {
            if (Phase != SessionPhase.WaitingToPitch || !State.IsTop) return false;

            var pitch = new PitchCall(zone, speedTier);
            var swing = _cpuBatter.DecideSwing(pitch, State, _rng);
            RecordContact(pitch, swing);
            _engine.ThrowPitch(pitch, swing);
            BeginBetweenPlays();
            return true;
        }

        public bool PlayerSwing()
        {
            if (Phase != SessionPhase.BallIncoming) return false;

            int offset = CurrentTick - IncomingArrivalTick;
            var swing = TouchToIntent.SwipeToSwing(offset);
            RecordContact(_incomingPitch, swing);
            _engine.ThrowPitch(_incomingPitch, swing);
            BeginBetweenPlays();
            return true;
        }

        private void ResolveTake()
        {
            LastContactWasSwing = false;
            LastContactFlight = null;
            _engine.ThrowPitch(_incomingPitch, SwingDecision.Take());
            BeginBetweenPlays();
        }

        private void RecordContact(PitchCall pitch, SwingDecision swing)
        {
            LastContactWasSwing = true;
            int abs = Math.Abs(swing.TimingOffsetTicks);
            int band = pitch.SpeedTier == PitchCall.MinSpeedTier ? 1 : 0;
            var resolution = _preResolver.Evaluate(pitch, swing, abs, band);
            LastOutcome = resolution.Outcome;
            LastContactFlight = resolution.Flight;
        }

        private void BeginBetweenPlays()
        {
            Phase = SessionPhase.BetweenPlays;
            _resumeTick = CurrentTick + BetweenPlaysTicks;
            CheckMatchOver();
        }

        private void BeginNextPlay()
        {
            if (State.Phase == MatchPhase.Finished)
            {
                ApplyRewardsIfPending();
                Phase = SessionPhase.MatchOver;
                return;
            }

            if (State.IsTop)
            {
                Phase = SessionPhase.WaitingToPitch;
            }
            else
            {
                _incomingPitch = _cpuPitcher.SelectPitch(State, _rng);
                FlightTicks = _incomingPitch.SpeedTier switch
                {
                    0 => SlowFlightTicks,
                    2 => FastFlightTicks,
                    _ => NormalFlightTicks,
                };
                IncomingArrivalTick = CurrentTick + FlightTicks;
                Phase = SessionPhase.BallIncoming;
            }
        }

        private void CheckMatchOver()
        {
            if (State.Phase == MatchPhase.Finished) ApplyRewardsIfPending();
        }

        private void ApplyRewardsIfPending()
        {
            if (_rewardsApplied || _rewards == null) return;
            _rewardsApplied = true;

            var stats = new MatchStats();
            foreach (var e in _engine.DrainEvents())
            {
                stats.Observe(e);
            }
            _rewards.ApplyPostMatch(State.Result, stats);
        }
    }
}
