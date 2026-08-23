using System;

namespace DiamondTilt.Core
{
    public readonly struct PitchCall
    {
        public const int MinSpeedTier = 0;
        public const int MaxSpeedTier = 2;

        public readonly int Zone;
        public readonly int SpeedTier;

        public PitchCall(int zone, int speedTier = 1)
        {
            Zone = zone;
            SpeedTier = Math.Clamp(speedTier, MinSpeedTier, MaxSpeedTier);
        }
    }

    public readonly struct SwingDecision
    {
        public readonly bool Took;
        public readonly int TimingOffsetTicks;

        public SwingDecision(bool took, int timingOffsetTicks)
        {
            Took = took;
            TimingOffsetTicks = timingOffsetTicks;
        }

        public static SwingDecision Take() => new SwingDecision(false, 0);
        public static SwingDecision Swing(int offsetTicks) => new SwingDecision(true, offsetTicks);
    }
}
