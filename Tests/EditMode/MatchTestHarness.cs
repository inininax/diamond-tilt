using DiamondTilt.Core;

namespace DiamondTilt.Tests
{
    public sealed class StubContactModel : IContactModel
    {
        private readonly PlayOutcome _outcome;

        public StubContactModel(PlayOutcome outcome)
        {
            _outcome = outcome;
        }

        public PlayOutcome Roll() => _outcome;
    }

    public static class MatchTestHarness
    {
        public const int CenterZone = 4;
        public const int CornerZone = 9;

        public static MatchEngine Engine(PlayOutcome contactOutcome = PlayOutcome.Single)
            => new MatchEngine(new StubContactModel(contactOutcome));

        public static void PlaceRunners(MatchEngine engine, bool first, bool second, bool third)
        {
            engine.State.FirstBase = first;
            engine.State.SecondBase = second;
            engine.State.ThirdBase = third;
        }

        public static void TakeBall(MatchEngine engine)
            => engine.ThrowPitch(new PitchCall(CornerZone), SwingDecision.Take());

        public static void TakeStrike(MatchEngine engine)
            => engine.ThrowPitch(new PitchCall(CenterZone), SwingDecision.Take());

        public static void ContactPitch(MatchEngine engine)
            => engine.ThrowPitch(new PitchCall(CenterZone), SwingDecision.Swing(0));

        public static void StrikeOutBatter(MatchEngine engine)
        {
            TakeStrike(engine);
            TakeStrike(engine);
            TakeStrike(engine);
        }
    }
}
