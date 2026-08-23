using System;

namespace DiamondTilt.Core
{
    public sealed class SwipeRecognizer
    {
        private readonly float _minDistancePx;
        private readonly int _maxDurationMs;
        private readonly int _minDurationMs;

        private bool _active;
        private float _startX, _startY;
        private long _startTick;

        public SwipeRecognizer(float minDistancePx = 40f, int minDurationMs = 40, int maxDurationMs = 800)
        {
            if (minDistancePx <= 0) throw new ArgumentOutOfRangeException(nameof(minDistancePx));
            if (maxDurationMs < minDurationMs) throw new ArgumentOutOfRangeException(nameof(maxDurationMs));

            _minDistancePx = minDistancePx;
            _minDurationMs = minDurationMs;
            _maxDurationMs = maxDurationMs;
        }

        public void Begin(float x, float y, long tickMs)
        {
            _active = true;
            _startX = x;
            _startY = y;
            _startTick = tickMs;
        }

        public SwipeGesture End(float x, float y, long tickMs)
        {
            if (!_active) return null;
            _active = false;

            float dx = x - _startX;
            float dy = y - _startY;
            float distance = (float)Math.Sqrt((double)dx * dx + (double)dy * dy);
            int durationMs = (int)Math.Max(0, tickMs - _startTick);

            if (distance < _minDistancePx) return null;
            if (durationMs > _maxDurationMs) return null;
            if (durationMs < 0) return null;

            return new SwipeGesture(dx, dy, distance, durationMs);
        }

        public void Cancel() => _active = false;
    }

    public sealed class SwipeGesture
    {
        public float Dx { get; }
        public float Dy { get; }
        public float DistancePx { get; }
        public int DurationMs { get; }
        public float DirectionDeg { get; }
        public float SpeedPxPerMs { get; }

        internal SwipeGesture(float dx, float dy, float distancePx, int durationMs)
        {
            Dx = dx;
            Dy = dy;
            DistancePx = distancePx;
            DurationMs = durationMs;
            DirectionDeg = (float)(Math.Atan2(dy, dx) * (180.0 / Math.PI));
            SpeedPxPerMs = distancePx / Math.Max(1, durationMs);
        }
    }

    public static class ZoneGrid
    {
        public const int Columns = 3;
        public const int Rows = 3;

        public static int FromNormalized(float nx, float ny)
        {
            if (nx < 0 || nx >= 1 || ny < 0 || ny >= 1)
                throw new ArgumentOutOfRangeException($"normalized coords out of [0,1): {nx},{ny}");

            int col = (int)(nx * Columns);
            int row = (int)(ny * Rows);
            return row * Columns + col;
        }
    }
}
