using System;
using System.Collections.Generic;

namespace DiamondTilt.Core
{
    public enum EconomyEventType
    {
        BalanceGranted,
        BalanceSpent,
        PurchaseCompleted,
        RewardClaimed,
        XpGained,
        EntitlementChanged
    }

    public readonly struct EconomyEvent
    {
        public readonly EconomyEventType Type;
        public readonly string Subject;

        public EconomyEvent(EconomyEventType type, string subject)
        {
            Type = type;
            Subject = subject ?? "";
        }
    }

    public sealed class EconomyEventBus
    {
        private List<EconomyEvent> _events = new List<EconomyEvent>();
        private List<EconomyEvent> _spare = new List<EconomyEvent>();

        public void Publish(EconomyEventType type, string subject)
            => _events.Add(new EconomyEvent(type, subject));

        public IReadOnlyList<EconomyEvent> Drain()
        {
            var drained = _events;
            _events = _spare;
            _spare = drained;
            _events.Clear();
            return drained;
        }
    }
}
