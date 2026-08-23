using System;

namespace DiamondTilt.Core
{
    public static class FieldConstants
    {
        public const double GravityMps2 = 9.81;
        public const double WallDistanceM = 100.0;
        public const double WallHeightM = 3.0;
        public const double FoulAngleDeg = 45.0;
        public const double DragK = 0.003;
        public const int IntegrationHz = 240;
        public const double ContactHeightM = 1.0;
    }

    public readonly struct LaunchParams
    {
        public readonly double ExitSpeedMps;
        public readonly double LaunchAngleDeg;
        public readonly double DirectionDeg;

        public LaunchParams(double exitSpeedMps, double launchAngleDeg, double directionDeg)
        {
            if (exitSpeedMps <= 0 || exitSpeedMps > 80) throw new ArgumentOutOfRangeException(nameof(exitSpeedMps));
            if (launchAngleDeg < 0 || launchAngleDeg > 90) throw new ArgumentOutOfRangeException(nameof(launchAngleDeg));
            if (directionDeg < -90 || directionDeg > 90) throw new ArgumentOutOfRangeException(nameof(directionDeg));

            ExitSpeedMps = exitSpeedMps;
            LaunchAngleDeg = launchAngleDeg;
            DirectionDeg = directionDeg;
        }
    }

    public readonly struct TrajectoryResult
    {
        public readonly Vec3 LandingPoint;
        public readonly double FlightTimeSeconds;
        public readonly double ApexHeightM;
        public readonly double DistanceM;
        public readonly bool CrossedWallPlane;

        public TrajectoryResult(Vec3 landingPoint, double flightTimeSeconds, double apexHeightM, double distanceM, bool crossedWallPlane)
        {
            LandingPoint = landingPoint;
            FlightTimeSeconds = flightTimeSeconds;
            ApexHeightM = apexHeightM;
            DistanceM = distanceM;
            CrossedWallPlane = crossedWallPlane;
        }
    }

    public static class BallFlight
    {
        private static readonly double DegToRad = Math.PI / 180.0;

        public static Vec3 InitialVelocity(LaunchParams p)
        {
            double angleRad = p.LaunchAngleDeg * DegToRad;
            double dirRad = p.DirectionDeg * DegToRad;
            double horizontal = p.ExitSpeedMps * Math.Cos(angleRad);
            return new Vec3(
                horizontal * Math.Sin(dirRad),
                p.ExitSpeedMps * Math.Sin(angleRad),
                horizontal * Math.Cos(dirRad));
        }

        public static Vec3 PositionNoDrag(LaunchParams p, double t)
        {
            Vec3 v = InitialVelocity(p);
            var origin = new Vec3(0, FieldConstants.ContactHeightM, 0);
            return new Vec3(
                origin.X + v.X * t,
                origin.Y + v.Y * t - 0.5 * FieldConstants.GravityMps2 * t * t,
                origin.Z + v.Z * t);
        }

        public static double FlightTimeNoDrag(LaunchParams p)
        {
            double vy = InitialVelocity(p).Y;
            double y0 = FieldConstants.ContactHeightM;
            double disc = vy * vy + 2 * FieldConstants.GravityMps2 * y0;
            if (disc < 0) return 0;
            return (vy + Math.Sqrt(disc)) / FieldConstants.GravityMps2;
        }

        public static double LandingDistanceNoDrag(LaunchParams p)
        {
            double h = InitialVelocity(p).HorizontalDistance();
            return h * FlightTimeNoDrag(p);
        }

        public static double ApexHeightNoDrag(LaunchParams p)
        {
            double vy = InitialVelocity(p).Y;
            return FieldConstants.ContactHeightM + vy * vy / (2 * FieldConstants.GravityMps2);
        }

        public static bool IsFoul(LaunchParams p)
            => Math.Abs(p.DirectionDeg) > FieldConstants.FoulAngleDeg;

        public static TrajectoryResult IntegrateWithDrag(LaunchParams p, double dragK = FieldConstants.DragK)
        {
            if (dragK < 0) throw new ArgumentOutOfRangeException(nameof(dragK));
            double dt = 1.0 / FieldConstants.IntegrationHz;
            Vec3 pos = new Vec3(0, FieldConstants.ContactHeightM, 0);
            Vec3 vel = InitialVelocity(p);
            double apex = pos.Y;
            double time = 0;
            bool crossedWall = false;

            while (pos.Y > 0 && time < 30)
            {
                Vec3 next = Step(pos, vel, dt);
                Vec3 nextVel = ApplyDrag(vel, dt, dragK);
                time += dt;
                if (next.Y > apex) apex = next.Y;

                if (!crossedWall && pos.HorizontalDistance() < FieldConstants.WallDistanceM
                    && next.HorizontalDistance() >= FieldConstants.WallDistanceM)
                {
                    crossedWall = HeightAtWallCrossing(pos, next) >= FieldConstants.WallHeightM;
                }

                if (next.Y <= 0)
                {
                    double frac = pos.Y / (pos.Y - next.Y);
                    var landing = new Vec3(
                        Lerp(pos.X, next.X, frac),
                        0,
                        Lerp(pos.Z, next.Z, frac));
                    return new TrajectoryResult(landing, time - dt + dt * frac, apex,
                        landing.HorizontalDistance(), crossedWall);
                }

                pos = next;
                vel = nextVel;
            }

            return new TrajectoryResult(pos, time, apex, pos.HorizontalDistance(), crossedWall);
        }

        public static bool ClearsWallForHomerun(LaunchParams p)
            => !IsFoul(p) && IntegrateWithDrag(p).CrossedWallPlane;

        public static double SpeedForLandingDistance(double targetDistanceM, double launchAngleDeg, double directionDeg)
        {
            if (targetDistanceM <= 0) throw new ArgumentOutOfRangeException(nameof(targetDistanceM));

            double low = 1.0, high = 79.0;
            for (int i = 0; i < 50; i++)
            {
                double mid = (low + high) / 2;
                double d = IntegrateWithDrag(new LaunchParams(mid, launchAngleDeg, directionDeg)).DistanceM;
                if (d < targetDistanceM) low = mid;
                else high = mid;
            }
            return (low + high) / 2;
        }

        private static Vec3 Step(Vec3 pos, Vec3 vel, double dt)
            => new Vec3(pos.X + vel.X * dt, pos.Y + vel.Y * dt, pos.Z + vel.Z * dt);

        private static Vec3 ApplyDrag(Vec3 vel, double dt, double dragK)
        {
            double speed = vel.Length();
            double dragFactor = 1 - dragK * speed * dt;
            if (dragFactor < 0) dragFactor = 0;
            return new Vec3(
                vel.X * dragFactor,
                vel.Y * dragFactor - FieldConstants.GravityMps2 * dt,
                vel.Z * dragFactor);
        }

        private static double HeightAtWallCrossing(Vec3 before, Vec3 after)
        {
            double d0 = before.HorizontalDistance();
            double d1 = after.HorizontalDistance();
            double frac = (FieldConstants.WallDistanceM - d0) / (d1 - d0);
            return Lerp(before.Y, after.Y, frac);
        }

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;
    }
}
