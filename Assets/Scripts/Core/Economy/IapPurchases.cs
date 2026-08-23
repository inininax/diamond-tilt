using System;
using System.Collections.Generic;

namespace DiamondTilt.Core.Economy
{
    public interface IReceiptValidator
    {
        bool Validate(string productId, string receiptPayload);
    }

    public sealed class FakeReceiptValidator : IReceiptValidator
    {
        public bool Validate(string productId, string receiptPayload)
            => !string.IsNullOrEmpty(productId)
               && !string.IsNullOrEmpty(receiptPayload)
               && receiptPayload != "TAMPERED";
    }

    [Serializable]
    public sealed class SubscriptionState
    {
        public string ExpiryDayKey = "";
    }

    public sealed class EntitlementService
    {
        private readonly IClock _clock;

        public SubscriptionState State { get; }

        public EntitlementService(SubscriptionState state, IClock clock)
        {
            State = state ?? new SubscriptionState();
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public bool HasActiveSubscription()
        {
            if (string.IsNullOrEmpty(State.ExpiryDayKey)) return false;
            if (!TimeKeys.TryParseDayKey(State.ExpiryDayKey, out var expiry)) return false;
            return expiry > TimeKeys.Today(_clock.UtcNow);
        }

        public void ActivateOrExtend(int days)
        {
            if (days <= 0) throw new ArgumentOutOfRangeException(nameof(days));

            DateTime baseDate = TimeKeys.Today(_clock.UtcNow);
            if (TimeKeys.TryParseDayKey(State.ExpiryDayKey, out var current) && current > baseDate)
                baseDate = current;

            State.ExpiryDayKey = TimeKeys.DayKey(baseDate.AddDays(days));
        }
    }

    public static class IapCatalog
    {
        public const string GemsSmall = "iap_gems_60";
        public const string GemsMedium = "iap_gems_300";
        public const string GemsLarge = "iap_gems_1000";
        public const string DiamondPass = "sub_diamond_pass_monthly";
        public const string SeasonPremium = "iap_season_premium";

        private static readonly Dictionary<string, long> GemPacks = new Dictionary<string, long>
        {
            [GemsSmall] = 60,
            [GemsMedium] = 320,
            [GemsLarge] = 1100,
        };

        public const int SubscriptionDays = 30;

        public static bool TryGetGemPack(string productId, out long gems)
            => GemPacks.TryGetValue(productId, out gems);
    }

    public sealed class IapPurchaseService
    {
        private readonly Wallet _wallet;
        private readonly EntitlementService _entitlements;
        private readonly SeasonPassSystem _season;
        private readonly IReceiptValidator _validator;
        private readonly IClock _clock;
        private readonly HashSet<string> _completedOrders = new HashSet<string>();

        public IapPurchaseService(
            Wallet wallet,
            EntitlementService entitlements,
            SeasonPassSystem season,
            IReceiptValidator validator,
            IClock clock)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _entitlements = entitlements ?? throw new ArgumentNullException(nameof(entitlements));
            _season = season ?? throw new ArgumentNullException(nameof(season));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public IReadOnlyCollection<string> CompletedOrders => _completedOrders;

        public PurchaseResult CompletePurchase(string productId, string receiptPayload, string orderId)
        {
            if (string.IsNullOrEmpty(productId) || string.IsNullOrEmpty(orderId)) return PurchaseResult.InvalidInput;
            if (_completedOrders.Contains(orderId)) return PurchaseResult.DuplicateOrder;
            if (!_validator.Validate(productId, receiptPayload)) return PurchaseResult.InvalidInput;

            if (IapCatalog.TryGetGemPack(productId, out long gems))
            {
                _wallet.Grant(CurrencyType.Gems, gems, $"iap:{productId}:{orderId}", _clock);
                _completedOrders.Add(orderId);
                return PurchaseResult.Success;
            }

            switch (productId)
            {
                case IapCatalog.DiamondPass:
                    _entitlements.ActivateOrExtend(IapCatalog.SubscriptionDays);
                    break;
                case IapCatalog.SeasonPremium:
                    _season.SetPremiumOwned(true);
                    break;
                default:
                    return PurchaseResult.UnknownItem;
            }

            _completedOrders.Add(orderId);
            return PurchaseResult.Success;
        }

        public void RestoreOrders(IEnumerable<string> orders)
        {
            foreach (var o in orders ?? Array.Empty<string>()) _completedOrders.Add(o);
        }
    }
}
