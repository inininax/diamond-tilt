using System;

namespace DiamondTilt.Core
{
    public readonly struct ContactResolution
    {
        public readonly PlayOutcome Outcome;
        public readonly LaunchParams? Flight;

        public ContactResolution(PlayOutcome outcome, LaunchParams? flight = null)
        {
            Outcome = outcome;
            Flight = flight;
        }
    }

    public interface IContactResolver
    {
        ContactResolution Evaluate(PitchCall pitch, SwingDecision swing, int absOffsetTicks, int perfectBandTicks);
    }

    public sealed class WeightedContactResolver : IContactResolver
    {
        private readonly IContactModel _model;

        public WeightedContactResolver(IContactModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public ContactResolution Evaluate(PitchCall pitch, SwingDecision swing, int absOffsetTicks, int perfectBandTicks)
            => new ContactResolution(_model.Roll());
    }

    public sealed class TimingContactModel : IContactResolver
    {
        private const int MeatZone = 4;

        public ContactResolution Evaluate(PitchCall pitch, SwingDecision swing, int absOffsetTicks, int perfectBandTicks)
        {
            bool zoneStrike = StrikeZone.IsStrike(pitch.Zone);
            int band = Math.Min(Math.Max(perfectBandTicks, 0), 1);
            if (absOffsetTicks <= band)
                return PerfectContact(pitch.Zone, zoneStrike);

            switch (absOffsetTicks)
            {
                case 1:
                    return zoneStrike
                        ? new ContactResolution(PlayOutcome.Double, Flight(31, 24))
                        : new ContactResolution(PlayOutcome.LineSingle, Flight(28, 16));
                case 2:
                    return new ContactResolution(
                        zoneStrike ? PlayOutcome.Single : PlayOutcome.Foul,
                        zoneStrike ? Flight(25, 12) : null);
                default:
                    return new ContactResolution(PlayOutcome.Foul);
            }
        }

        private static ContactResolution PerfectContact(int zone, bool zoneStrike)
        {
            if (!zoneStrike) return Drive(29, 40);
            if (zone == MeatZone) return Drive(40, 33);
            return Drive(33, 38);
        }

        private static ContactResolution Drive(double speed, double angle)
        {
            var flight = Flight(speed, angle);
            return BallFlight.ClearsWallForHomerun(flight.Value)
                ? new ContactResolution(PlayOutcome.Homerun, flight)
                : new ContactResolution(PlayOutcome.DeepFly, flight);
        }

        private static LaunchParams? Flight(double speed, double angle)
            => new LaunchParams(speed, angle, 0);
    }
}
