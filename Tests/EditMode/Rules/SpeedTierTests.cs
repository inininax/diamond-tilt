using DiamondTilt.Core;
using DiamondTilt.Core.Economy;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class SpeedTierTests
    {
        [Test]
        public void FastPitch_ShrinksMissWindow_OffsetThreeIsWhiff()
        {
            var engine = new MatchEngine(new StubContactModel(PlayOutcome.Single));

            engine.ThrowPitch(new PitchCall(4, 2), SwingDecision.Swing(3));

            Assert.That(engine.State.Strikes, Is.EqualTo(1));
            Assert.That(engine.State.Outs, Is.EqualTo(0));
        }

        [Test]
        public void SlowPitch_NormalWindow_OffsetThreeIsFoul()
        {
            var engine = new MatchEngine(new StubContactModel(PlayOutcome.Foul));

            engine.ThrowPitch(new PitchCall(4, 0), SwingDecision.Swing(3));

            Assert.That(engine.State.Outs, Is.EqualTo(0));
            Assert.That(engine.DrainEvents(), Has.Some.Matches<MatchEvent>(e => e.Type == MatchEventType.StrikeCalled));
        }

        [Test]
        public void SlowPitch_WidensPerfectBand_OffsetOneBarrels()
        {
            var model = new TimingContactModel();
            var slow = model.Evaluate(new PitchCall(4, 0), SwingDecision.Swing(1), 1, 1);
            var normal = model.Evaluate(new PitchCall(4, 1), SwingDecision.Swing(1), 1, 0);

            Assert.That(slow.Outcome == PlayOutcome.Homerun || slow.Outcome == PlayOutcome.DeepFly, Is.True);
            Assert.That(normal.Outcome, Is.EqualTo(PlayOutcome.Double));
        }

        [Test]
        public void PitchCall_SpeedTier_ClampedToValidRange()
        {
            Assert.That(new PitchCall(4, -5).SpeedTier, Is.EqualTo(0));
            Assert.That(new PitchCall(4, 9).SpeedTier, Is.EqualTo(2));
            Assert.That(new PitchCall(4).SpeedTier, Is.EqualTo(1));
        }

        [Test]
        public void PitcherAI_MixesSpeedTiers()
        {
            var rng = new Mulberry32Rng(21u);
            var pitcher = new SeededPitcherAI();
            var state = new MatchState();

            bool sawSlow = false, sawFast = false;
            for (int i = 0; i < 60; i++)
            {
                var pitch = pitcher.SelectPitch(state, rng);
                if (pitch.SpeedTier == 0) sawSlow = true;
                if (pitch.SpeedTier == 2) sawFast = true;
            }

            Assert.That(sawSlow && sawFast, Is.True);
        }
    }
}
