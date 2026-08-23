using System;
using DiamondTilt.Core;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class TouchInputTests
    {
        [Test]
        public void Recognizer_TooShortSwipe_Rejected()
        {
            var r = new SwipeRecognizer(minDistancePx: 40);
            r.Begin(100, 100, 0);

            Assert.That(r.End(120, 110, 200), Is.Null);
        }

        [Test]
        public void Recognizer_ValidSwipe_ProducesGesture()
        {
            var r = new SwipeRecognizer();
            r.Begin(100, 300, 1000);

            var g = r.End(160, 240, 1150);

            Assert.That(g, Is.Not.Null);
            Assert.That(g.DistancePx, Is.EqualTo(84.8528f).Within(0.01f));
            Assert.That(g.DurationMs, Is.EqualTo(150));
            Assert.That(g.DirectionDeg, Is.EqualTo(-45f).Within(0.1f));
        }

        [Test]
        public void Recognizer_TooSlowSwipe_Rejected()
        {
            var r = new SwipeRecognizer(maxDurationMs: 800);
            r.Begin(0, 0, 0);

            Assert.That(r.End(500, 500, 900), Is.Null);
        }

        [Test]
        public void Recognizer_Boundaries_ExactlyAtLimits_Accepted()
        {
            var r = new SwipeRecognizer(minDistancePx: 40, minDurationMs: 40, maxDurationMs: 800);
            r.Begin(0, 0, 1000);

            var g = r.End(40, 0, 1800);

            Assert.That(g, Is.Not.Null);
            Assert.That(g.DurationMs, Is.EqualTo(800));
        }

        [Test]
        public void Recognizer_EndWithoutBegin_ReturnsNull_AndCancelWorks()
        {
            var r = new SwipeRecognizer();

            Assert.That(r.End(50, 50, 100), Is.Null);

            r.Begin(0, 0, 0);
            r.Cancel();
            Assert.That(r.End(400, 400, 100), Is.Null);
        }

        [Test]
        public void ZoneGrid_MapsNineCells()
        {
            Assert.That(ZoneGrid.FromNormalized(0.1f, 0.1f), Is.EqualTo(0));
            Assert.That(ZoneGrid.FromNormalized(0.5f, 0.5f), Is.EqualTo(4));
            Assert.That(ZoneGrid.FromNormalized(0.9f, 0.9f), Is.EqualTo(8));
            Assert.That(ZoneGrid.FromNormalized(0.1f, 0.5f), Is.EqualTo(3));
        }

        [Test]
        public void ZoneGrid_OutOfRange_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ZoneGrid.FromNormalized(-0.1f, 0.5f));
            Assert.Throws<ArgumentOutOfRangeException>(() => ZoneGrid.FromNormalized(0.5f, 1f));
        }

        [Test]
        public void SwipeToSwing_ClampsToEngineWindow()
        {
            Assert.That(TouchToIntent.SwipeToSwing(50).TimingOffsetTicks,
                Is.EqualTo(TouchToIntent.MaxOffsetTicks));
            Assert.That(TouchToIntent.SwipeToSwing(-50).TimingOffsetTicks,
                Is.EqualTo(-TouchToIntent.MaxOffsetTicks));
            Assert.That(TouchToIntent.SwipeToSwing(2).TimingOffsetTicks, Is.EqualTo(2));
        }

        [Test]
        public void FlickSpeed_MapsToSpeedTier_Boundaries()
        {
            Assert.That(TouchToIntent.FlickToSpeedTier(0.5f), Is.EqualTo(0));
            Assert.That(TouchToIntent.FlickToSpeedTier(1f), Is.EqualTo(1));
            Assert.That(TouchToIntent.FlickToSpeedTier(2f), Is.EqualTo(2));
        }
    }
}
