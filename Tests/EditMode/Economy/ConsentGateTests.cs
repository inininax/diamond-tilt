using System;
using System.Collections.Generic;
using DiamondTilt.Core;
using DiamondTilt.Core.Economy;
using NUnit.Framework;

namespace DiamondTilt.Tests
{
    public sealed class ConsentGateTests
    {
        private sealed class RecordingSink : IAnalyticsSink
        {
            public int Calls;
            public string LastEvent;
            public void Track(string eventName, IReadOnlyDictionary<string, string> properties)
            {
                Calls++;
                LastEvent = eventName;
            }
        }

        [Test]
        public void DeniedConsent_DropsEvents()
        {
            var consent = new ConsentState { AnalyticsAllowed = false };
            var sink = new RecordingSink();
            var gated = new ConsentGatedAnalytics(() => consent, sink);

            gated.Track("match_end", null);

            Assert.That(sink.Calls, Is.EqualTo(0));
        }

        [Test]
        public void GrantedConsent_PassesEvents()
        {
            var consent = new ConsentState { AnalyticsAllowed = true };
            var sink = new RecordingSink();
            var gated = new ConsentGatedAnalytics(() => consent, sink);

            gated.Track("match_end", null);

            Assert.That(sink.Calls, Is.EqualTo(1));
            Assert.That(sink.LastEvent, Is.EqualTo("match_end"));
        }

        [Test]
        public void ConsentRevokedMidSession_TakesEffectImmediately()
        {
            var state = new ConsentState { AnalyticsAllowed = true };
            var sink = new RecordingSink();
            var gated = new ConsentGatedAnalytics(() => state, sink);

            gated.Track("a", null);
            state.AnalyticsAllowed = false;
            gated.Track("b", null);

            Assert.That(sink.Calls, Is.EqualTo(1));
        }

        [Test]
        public void NullConstructorArgs_Rejected()
        {
            var state = new ConsentState();
            Assert.Throws<ArgumentNullException>(() => new ConsentGatedAnalytics(null, NullAnalyticsSink.Instance));
            Assert.Throws<ArgumentNullException>(() => new ConsentGatedAnalytics(() => state, null));
        }
    }
}
