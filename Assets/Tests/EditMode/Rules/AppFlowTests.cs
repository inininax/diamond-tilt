using System;
using DiamondTilt.Core;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class AppFlowTests
    {
        [Test]
        public void AttachMatch_EntersMatchScreen()
        {
            var flow = new AppFlowController();

            AppScreen seen = AppScreen.Boot;
            flow.ScreenChanged += s => seen = s;
            flow.AttachMatch(new MatchEngine(new TimingContactModel()));

            Assert.That(flow.Current, Is.EqualTo(AppScreen.Match));
            Assert.That(seen, Is.EqualTo(AppScreen.Match));
        }

        [Test]
        public void PumpEvents_MatchEnded_AutoNavigatesToResult_WithWinner()
        {
            var engine = new MatchEngine(new TimingContactModel());
            for (int i = 0; i < 18; i++) MatchTestHarness.StrikeOutBatter(engine);

            var flow = new AppFlowController();
            flow.AttachMatch(engine);

            flow.PumpEvents();

            Assert.That(flow.Current, Is.EqualTo(AppScreen.Result));
            Assert.That(flow.LastResult, Is.EqualTo(Winner.Draw));
        }

        [Test]
        public void ResultScreen_BlocksUntilMatchActuallyFinished()
        {
            var engine = new MatchEngine(new TimingContactModel());
            var flow = new AppFlowController();
            flow.AttachMatch(engine);

            Assert.That(flow.GoTo(AppScreen.Result), Is.False);
            Assert.That(flow.Current, Is.EqualTo(AppScreen.Match));
        }

        [Test]
        public void TitleAndSettings_TransitionsLegal()
        {
            var flow = new AppFlowController();

            Assert.That(flow.GoTo(AppScreen.Title), Is.True);
            Assert.That(flow.GoTo(AppScreen.Settings), Is.True);
            Assert.That(flow.GoTo(AppScreen.Result), Is.False);
        }

        [Test]
        public void Settings_FromMatch_Rejected_FromResult_Accepted()
        {
            var engine = new MatchEngine(new TimingContactModel());
            var flow = new AppFlowController();
            flow.AttachMatch(engine);
            for (int i = 0; i < 18; i++) MatchTestHarness.StrikeOutBatter(engine);

            Assert.That(flow.GoTo(AppScreen.Settings), Is.False);

            flow.PumpEvents();
            Assert.That(flow.GoTo(AppScreen.Settings), Is.True);
            Assert.That(flow.Current, Is.EqualTo(AppScreen.Settings));
        }

        [Test]
        public void GoToTitle_MidMatch_QuitsToTitle()
        {
            var engine = new MatchEngine(new TimingContactModel());
            var flow = new AppFlowController();
            flow.AttachMatch(engine);

            Assert.That(flow.GoTo(AppScreen.Title), Is.True);
        }

        [Test]
        public void PumpEvents_NoEngineAttached_IsNoOp()
        {
            var flow = new AppFlowController();

            Assert.DoesNotThrow(() => flow.PumpEvents());
        }
    }
}
