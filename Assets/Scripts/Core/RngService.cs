using System;

namespace DiamondTilt.Core
{
    public interface IRngService
    {
        double NextDouble();
        int NextInt(int maxExclusive);
    }

    public sealed class Mulberry32Rng : IRngService
    {
        private uint _state;

        public Mulberry32Rng(uint seed)
        {
            _state = seed;
        }

        public double NextDouble()
        {
            _state = unchecked(_state + 0x6D2B79F5u);
            uint t = _state;
            t = unchecked((t ^ (t >> 15)) * (t | 1u));
            t ^= unchecked(t + (t ^ (t >> 7)) * (t | 61u));
            uint result = t ^ (t >> 14);
            return result / 4294967296.0;
        }

        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return (int)(NextDouble() * maxExclusive);
        }
    }
}
