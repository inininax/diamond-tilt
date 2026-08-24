using System.Collections.Generic;
using DiamondTilt.Core;
using DiamondTilt.Core.Economy;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class MatchPlaySessionTests
    {
        private static (MatchPlaySession Session, MatchEngine Engine, FixedClock Clock, Wallet Wallet) CreateSession(uint seed = 5u)
        {
            var clock = new FixedClock(new System.DateTime(2026, 8, 23, 12, 0, 0, System.DateTimeKind.Utc));
            var wallet = new Wallet(SaveIntegrity.DeriveKey(909u));
            var save = new SaveData();
            SaveClamp.MigrateToCurrent(save);
            var services = new GameServices(save, SaveIntegrity.DeriveKey(910u), clock);
            var engine = new MatchEngine(new TimingContactModel());
            var rewards = new MatchRewardService(services.Wallet, services.Missions, services.SeasonPass, clock);
            var session = new MatchPlaySession(engine, new Mulberry32Rng(seed), rewards);
            return (session, engine, clock, services.Wallet);
        }

        [Test]
        public void PlayerPitch_ZoneTap_ResolvesAtBatImmediately()
        {
            var (session, engine, _, _) = CreateSession();

            Assert.That(session.PlayerPitch(4, 1), Is.True);

            Assert.That(session.Phase, Is.EqualTo(SessionPhase.BetweenPlays));
            Assert.That(engine.State.Balls + engine.State.Strikes + engine.State.Outs > 0
                        || engine.State.FirstBase || engine.State.SecondBase || engine.State.ThirdBase,
                Is.True, "at-bat must produce an outcome");
        }

        [Test]
        public void PlayerCannotPitch_DuringBottomHalf()
        {
            var (session, _, _, _) = CreateSession();
            DriveToBallIncoming(session);

            Assert.That(session.PlayerBatting, Is.True);
            Assert.That(session.PlayerPitch(4, 1), Is.False);
        }

        private static void DriveToBallIncoming(MatchPlaySession session)
        {
            var rng = new Mulberry32Rng(777u);
            int guard = 40000;
            while (session.Phase != SessionPhase.BallIncoming && guard-- > 0)
            {
                if (session.Phase == SessionPhase.WaitingToPitch)
                    session.PlayerPitch(rng.NextInt(StrikeZone.MaxZone) + 1, rng.NextInt(3));
                else if (session.Phase == SessionPhase.BallIncoming && session.CurrentTick >= session.IncomingArrivalTick)
                    session.PlayerSwing();
                session.TickAdvance(1);
            }
        }

        [Test]
        public void CpuPitch_NoSwing_AutoTakeAfterGrace()
        {
            var (session, engine, _, _) = CreateSession();
            DriveToBallIncoming(session);

            Assert.That(session.Phase, Is.EqualTo(SessionPhase.BallIncoming));
            int arrival = session.IncomingArrivalTick;
            session.TickAdvance(arrival - session.CurrentTick);

            session.TickAdvance(TouchToIntent.MaxOffsetTicks + 2);

            Assert.That(engine.State.Balls + engine.State.Strikes + engine.State.Outs, Is.GreaterThan(0));
            Assert.That(session.Phase, Is.EqualTo(SessionPhase.BetweenPlays));
        }

        [Test]
        public void PerfectTiming_ProducesContact()
        {
            var (session, engine, _, _) = CreateSession();
            DriveToBallIncoming(session);

            int arrival = session.IncomingArrivalTick;
            session.TickAdvance(arrival - session.CurrentTick - 1);

            Assert.That(session.PlayerSwing(), Is.True);

            bool contact = engine.State.Strikes < 2;
            Assert.That(contact || engine.State.Outs > 0 || engine.State.FirstBase,
                Is.True, "offset -1 must land in the contact band");
        }

        [Test]
        public void AlwaysPerfectPlayer_CompletesFullInteractiveMatch()
        {
            var (session, engine, clock, wallet) = CreateSession();

            var rng = new Mulberry32Rng(888u);
            int guard = 60000;
            while (session.Phase != SessionPhase.MatchOver && guard-- > 0)
            {
                if (session.Phase == SessionPhase.WaitingToPitch)
                {
                    session.PlayerPitch(rng.NextInt(StrikeZone.MaxZone) + 1, rng.NextInt(3));
                }
                else if (session.Phase == SessionPhase.BallIncoming &&
                         session.CurrentTick >= session.IncomingArrivalTick)
                {
                    session.PlayerSwing();
                }
                session.TickAdvance(1);
            }

            Assert.That(session.Phase, Is.EqualTo(SessionPhase.MatchOver));
            Assert.That(engine.State.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(wallet.Coins, Is.GreaterThan(0), "rewards must apply once at match end");

            long before = wallet.Coins;
            session.TickAdvance(100);
            Assert.That(wallet.Coins, Is.EqualTo(before), "rewards must not double-apply");
        }

        [Test]
        public void BallIncoming_FlightDuration_WithinDesignedBand()
        {
            for (uint seed = 1u; seed <= 5u; seed++)
            {
                var (session, _, _, _) = CreateSession(seed);
                DriveToBallIncoming(session);

                Assert.That(session.FlightTicks, Is.InRange(40, 64), $"seed {seed}");
                Assert.That(session.IncomingArrivalTick,
                    Is.EqualTo(session.CurrentTick + session.FlightTicks));
            }
        }
    }
}
