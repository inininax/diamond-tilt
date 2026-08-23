using System;
using DiamondTilt.Core;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class BallFlightTests
    {
        private static LaunchParams Fly(double speed = 40, double angle = 35, double dir = 0)
            => new LaunchParams(speed, angle, dir);

        [Test]
        public void Vec3_Operators_ArithmeticCorrect()
        {
            var a = new Vec3(1, 2, 3);
            var b = new Vec3(4, 5, 6);

            Assert.That((a + b).X, Is.EqualTo(5));
            Assert.That((b - a).Z, Is.EqualTo(3));
            Assert.That((a * 2).Y, Is.EqualTo(4));
            Assert.That(new Vec3(3, 4, 0).Length(), Is.EqualTo(5));
            Assert.That(new Vec3(3, 0, 4).HorizontalDistance(), Is.EqualTo(5));
        }

        [Test]
        public void PositionNoDrag_AtZero_IsContactPoint()
        {
            var p = BallFlight.PositionNoDrag(Fly(), 0);

            Assert.That(p.Y, Is.EqualTo(FieldConstants.ContactHeightM));
            Assert.That(p.X, Is.EqualTo(0));
        }

        [Test]
        public void FlightTimeNoDrag_PositiveAndSane()
        {
            double t = BallFlight.FlightTimeNoDrag(Fly(speed: 40, angle: 35));

            Assert.That(t, Is.GreaterThan(1.0));
            Assert.That(t, Is.LessThan(10.0));
        }

        [Test]
        public void ApexHeight_MatchesClosedForm()
        {
            var launch = Fly(speed: 30, angle: 45);
            double vy = BallFlight.InitialVelocity(launch).Y;
            double expected = FieldConstants.ContactHeightM + vy * vy / (2 * FieldConstants.GravityMps2);

            Assert.That(BallFlight.ApexHeightNoDrag(launch), Is.EqualTo(expected).Within(1e-9));
        }

        [Test]
        public void LandingDistance_MonotonicInSpeed()
        {
            double slow = BallFlight.LandingDistanceNoDrag(Fly(speed: 25));
            double fast = BallFlight.LandingDistanceNoDrag(Fly(speed: 45));

            Assert.That(fast, Is.GreaterThan(slow));
        }

        [Test]
        public void FoulAngle_Boundaries()
        {
            Assert.That(BallFlight.IsFoul(Fly(dir: 45)), Is.False);
            Assert.That(BallFlight.IsFoul(Fly(dir: -45)), Is.False);
            Assert.That(BallFlight.IsFoul(Fly(dir: 46)), Is.True);
            Assert.That(BallFlight.IsFoul(Fly(dir: -60)), Is.True);
            Assert.That(BallFlight.IsFoul(Fly(dir: 0)), Is.False);
        }

        [Test]
        public void Drag_ShortensFlight_ComparedToNoDrag()
        {
            var launch = Fly(speed: 45, angle: 35);

            Assert.That(BallFlight.IntegrateWithDrag(launch).DistanceM,
                Is.LessThan(BallFlight.LandingDistanceNoDrag(launch)));
        }

        [Test]
        public void Drag_IntegrationDeterministicAcrossRuns()
        {
            var a = BallFlight.IntegrateWithDrag(Fly(speed: 42, angle: 28, dir: -12));
            var b = BallFlight.IntegrateWithDrag(Fly(speed: 42, angle: 28, dir: -12));

            Assert.That(a.DistanceM, Is.EqualTo(b.DistanceM));
            Assert.That(a.FlightTimeSeconds, Is.EqualTo(b.FlightTimeSeconds));
            Assert.That(a.LandingPoint.X, Is.EqualTo(b.LandingPoint.X));
        }

        [Test]
        public void Inversion_RoundTripsWithinTolerance()
        {
            foreach (var target in new[] { 40.0, 70.0, 99.0 })
            {
                double speed = BallFlight.SpeedForLandingDistance(target, 32, 0);
                double achieved = BallFlight.IntegrateWithDrag(new LaunchParams(speed, 32, 0)).DistanceM;

                Assert.That(achieved, Is.EqualTo(target).Within(0.05), $"target {target}m");
            }
        }

        [Test]
        public void ClearsWall_WeakFly_No_StrongFly_Yes()
        {
            Assert.That(BallFlight.ClearsWallForHomerun(Fly(speed: 25, angle: 40)), Is.False);
            Assert.That(BallFlight.ClearsWallForHomerun(Fly(speed: 48, angle: 32)), Is.True);
        }

        [Test]
        public void ClearsWall_FoulFly_NeverHomerun()
        {
            Assert.That(BallFlight.ClearsWallForHomerun(Fly(speed: 50, angle: 32, dir: 80)), Is.False);
        }

        [Test]
        public void Drag_NearZeroDrag_MatchesClosedForm_ParityGuard()
        {
            var launch = Fly(speed: 38, angle: 33);

            Assert.That(BallFlight.IntegrateWithDrag(launch, 1e-9).DistanceM,
                Is.EqualTo(BallFlight.LandingDistanceNoDrag(launch)).Within(0.5));
        }

        [Test]
        public void Validation_RejectsOutOfRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LaunchParams(0, 30, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LaunchParams(-5, 30, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LaunchParams(100, 30, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LaunchParams(40, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LaunchParams(40, 91, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LaunchParams(40, 30, -91));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LaunchParams(40, 30, 91));
        }
    }
}
