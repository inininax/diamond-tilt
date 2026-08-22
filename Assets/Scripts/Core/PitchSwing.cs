namespace DiamondTilt.Core
{
    public readonly struct PitchCall
    {
        public readonly int Zone;

        public PitchCall(int zone)
        {
            Zone = zone;
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
