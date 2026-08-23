using System;
using System.Collections.Generic;

namespace DiamondTilt.Core.Economy
{
    public sealed class ConsentState
    {
        public bool AnalyticsAllowed { get; set; }
        public bool AdsPersonalizationAllowed { get; set; }

        public static ConsentState DefaultDeny { get; } = new ConsentState();
    }

    public interface IAnalyticsSink
    {
        void Track(string eventName, IReadOnlyDictionary<string, string> properties);
    }

    public sealed class NullAnalyticsSink : IAnalyticsSink
    {
        public static readonly NullAnalyticsSink Instance = new NullAnalyticsSink();

        private NullAnalyticsSink()
        {
        }

        public void Track(string eventName, IReadOnlyDictionary<string, string> properties)
        {
        }
    }

    public sealed class ConsentGatedAnalytics : IAnalyticsSink
    {
        private readonly Func<ConsentState> _consent;
        private readonly IAnalyticsSink _inner;

        public ConsentGatedAnalytics(Func<ConsentState> consent, IAnalyticsSink inner)
        {
            _consent = consent ?? throw new ArgumentNullException(nameof(consent));
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public void Track(string eventName, IReadOnlyDictionary<string, string> properties)
        {
            if (!_consent().AnalyticsAllowed) return;
            _inner.Track(eventName, properties);
        }
    }
}
