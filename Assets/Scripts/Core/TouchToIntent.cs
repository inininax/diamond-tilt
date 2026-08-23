using System;

namespace DiamondTilt.Core
{
    public static class TouchToIntent
    {
        public const int MaxOffsetTicks = 6;

        public static SwingDecision SwipeToSwing(int ticksAfterIdealContact)
            => SwingDecision.Swing(Math.Clamp(ticksAfterIdealContact, -MaxOffsetTicks, MaxOffsetTicks));

        public static PitchCall TapToPitch(float normalizedX, float normalizedY)
            => new PitchCall(ZoneGrid.FromNormalized(normalizedX, normalizedY));

        public static int FlickToSpeedTier(float flickSpeedPxPerMs)
        {
            if (flickSpeedPxPerMs < 0) throw new ArgumentOutOfRangeException(nameof(flickSpeedPxPerMs));
            if (flickSpeedPxPerMs >= 2f) return PitchCall.MaxSpeedTier;
            return flickSpeedPxPerMs >= 1f ? 1 : PitchCall.MinSpeedTier;
        }
    }
}
