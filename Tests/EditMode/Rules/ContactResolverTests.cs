using System;
using DiamondTilt.Core;
using DiamondTilt.Core.Economy;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class ContactResolverTests
    {
        [Test]
        public void TimingModel_PerfectTiming_StrikeZone_PowerDrive()
        {
            var model = new TimingContactModel();
            var r = model.Evaluate(new PitchCall(4), SwingDecision.Swing(0), 0, 1);

            Assert.That(r.Outcome == PlayOutcome.Homerun || r.Outcome == PlayOutcome.DeepFly, Is.True);
            Assert.That(r.Flight.HasValue, Is.True);
        }

        [Test]
        public void TimingModel_PerfectTiming_MeatZone_ClearsWall_ForHomerun()
        {
            var model = new TimingContactModel();
            var r = model.Evaluate(new PitchCall(4), SwingDecision.Swing(0), 0, 1);

            Assert.That(r.Outcome, Is.EqualTo(PlayOutcome.Homerun));
        }

        [Test]
        public void AutoMatch_HomerunsOccur_AcrossSeededGames()
        {
            int homeruns = 0;
            for (uint seed = 600; seed < 610; seed++)
            {
                var engine = new MatchEngine(new TimingContactModel());
                AutoMatch.PlaySelfContained(engine, Difficulty.Normal, seed);
                foreach (var e in engine.DrainEvents())
                {
                    if (e.Type == MatchEventType.HomerunRecorded) homeruns++;
                }
            }

            Assert.That(homeruns, Is.GreaterThan(0));
        }

        [Test]
        public void TimingModel_NonStrikeZone_ReducesDrivePower()
        {
            var model = new TimingContactModel();
            var good = model.Evaluate(new PitchCall(4), SwingDecision.Swing(0), 0, 1);
            var chased = model.Evaluate(new PitchCall(9), SwingDecision.Swing(0), 0, 1);

            Assert.That(chased.Flight.Value.ExitSpeedMps,
                Is.LessThan(good.Flight.Value.ExitSpeedMps));
        }

        [Test]
        public void TimingModel_OffsetOne_StrikeZone_IsDouble()
        {
            var model = new TimingContactModel();
            var r = model.Evaluate(new PitchCall(4), SwingDecision.Swing(1), 1, 0);

            Assert.That(r.Outcome, Is.EqualTo(PlayOutcome.Double));
        }

        [Test]
        public void TimingModel_OffsetTwo_FairContact_Single()
        {
            var model = new TimingContactModel();
            var r = model.Evaluate(new PitchCall(4), SwingDecision.Swing(2), 2, 1);

            Assert.That(r.Outcome, Is.EqualTo(PlayOutcome.Single));
        }

        [Test]
        public void TimingModel_OffsetThree_Foul()
        {
            var model = new TimingContactModel();
            var r = model.Evaluate(new PitchCall(4), SwingDecision.Swing(-3), 3, 1);

            Assert.That(r.Outcome, Is.EqualTo(PlayOutcome.Foul));
        }

        [Test]
        public void TimingModel_Deterministic_NoRngConsumed()
        {
            var model = new TimingContactModel();
            var a = model.Evaluate(new PitchCall(7), SwingDecision.Swing(1), 1, 1);
            var b = model.Evaluate(new PitchCall(7), SwingDecision.Swing(1), 1, 1);

            Assert.That(a.Outcome, Is.EqualTo(b.Outcome));
            Assert.That(a.Flight.Value.ExitSpeedMps, Is.EqualTo(b.Flight.Value.ExitSpeedMps));
        }

        [Test]
        public void WeightedAdapter_PassesThroughTableModel()
        {
            var adapter = new WeightedContactResolver(new StubContactModel(PlayOutcome.Triple));
            var r = adapter.Evaluate(new PitchCall(4), SwingDecision.Swing(0), 0, 1);

            Assert.That(r.Outcome, Is.EqualTo(PlayOutcome.Triple));
        }

        [Test]
        public void Engine_DeepFlyFlight_ClearingWall_CountsAsHomerun()
        {
            var flight = new LaunchParams(52, 31, 0);
            Assert.That(BallFlight.ClearsWallForHomerun(flight), Is.True);
            var engine = new MatchEngine(new FixedResolver(PlayOutcome.DeepFly, flight));
            MatchTestHarness.ContactPitch(engine);

            Assert.That(engine.State.AwayRuns, Is.EqualTo(1));
            Assert.That(engine.State.FirstBase, Is.False);
        }

        [Test]
        public void Engine_DeepFlyFlight_ShortOfWall_IsCaughtOut()
        {
            var flight = new LaunchParams(22, 45, 0);
            Assert.That(BallFlight.ClearsWallForHomerun(flight), Is.False);
            var engine = new MatchEngine(new FixedResolver(PlayOutcome.DeepFly, flight));
            MatchTestHarness.ContactPitch(engine);

            Assert.That(engine.State.AwayRuns, Is.EqualTo(0));
            Assert.That(engine.State.Outs, Is.EqualTo(1));
        }

        private sealed class FixedResolver : IContactResolver
        {
            private readonly PlayOutcome _outcome;
            private readonly LaunchParams _flight;

            public FixedResolver(PlayOutcome outcome, LaunchParams flight)
            {
                _outcome = outcome;
                _flight = flight;
            }

            public ContactResolution Evaluate(PitchCall pitch, SwingDecision swing, int absOffsetTicks, int perfectBandTicks)
                => new ContactResolution(_outcome, _flight);
        }
    }
}
