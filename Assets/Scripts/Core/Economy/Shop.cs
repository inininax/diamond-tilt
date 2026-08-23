using System;
using System.Collections.Generic;

namespace DiamondTilt.Core
{
    public enum ShopItemKind
    {
        Cosmetic,
        Booster
    }

    public sealed class ShopItemDef
    {
        public string Id { get; }
        public ShopItemKind Kind { get; }
        public CurrencyType Currency { get; }
        public long Price { get; }

        public ShopItemDef(string id, ShopItemKind kind, CurrencyType currency, long price)
        {
            Id = id;
            Kind = kind;
            Currency = currency;
            Price = price;
        }
    }

    public static class ShopCatalog
    {
        private static readonly List<ShopItemDef> Items = new List<ShopItemDef>
        {
            new ShopItemDef("uniform_home", ShopItemKind.Cosmetic, CurrencyType.Gems, 100),
            new ShopItemDef("stadium_sunset", ShopItemKind.Cosmetic, CurrencyType.Gems, 250),
            new ShopItemDef("bat_lucky", ShopItemKind.Booster, CurrencyType.Coins, 500),
            new ShopItemDef("glove_pro", ShopItemKind.Booster, CurrencyType.Coins, 750),
        };

        public static IReadOnlyList<ShopItemDef> All => Items;

        public static bool TryGet(string itemId, out ShopItemDef def)
        {
            foreach (var item in Items)
            {
                if (item.Id == itemId) { def = item; return true; }
            }
            def = null;
            return false;
        }
    }

    public enum PurchaseResult
    {
        Success,
        DuplicateOrder,
        UnknownItem,
        InsufficientFunds,
        InvalidInput
    }

    public sealed class PurchaseProcessor
    {
        private readonly HashSet<string> _completedOrders = new HashSet<string>();
        private readonly HashSet<string> _ownedItems = new HashSet<string>();

        public IReadOnlyCollection<string> OwnedItems => _ownedItems;
        public IReadOnlyCollection<string> CompletedOrders => _completedOrders;

        public PurchaseResult TryPurchase(string itemId, string orderId, Wallet wallet, IClock clock)
        {
            if (string.IsNullOrEmpty(itemId) || string.IsNullOrEmpty(orderId) || wallet == null || clock == null)
                return PurchaseResult.InvalidInput;

            if (_completedOrders.Contains(orderId)) return PurchaseResult.DuplicateOrder;
            if (!ShopCatalog.TryGet(itemId, out var def)) return PurchaseResult.UnknownItem;
            if (def.Kind == ShopItemKind.Cosmetic && _ownedItems.Contains(itemId))
                return PurchaseResult.DuplicateOrder;

            try
            {
                wallet.Spend(def.Currency, def.Price, $"shop:{itemId}:{orderId}", clock);
            }
            catch (EconomyException)
            {
                return PurchaseResult.InsufficientFunds;
            }

            _completedOrders.Add(orderId);
            _ownedItems.Add(itemId);
            return PurchaseResult.Success;
        }

        public void RestoreState(IEnumerable<string> orders, IEnumerable<string> owned)
        {
            foreach (var o in orders ?? Array.Empty<string>()) _completedOrders.Add(o);
            foreach (var i in owned ?? Array.Empty<string>()) _ownedItems.Add(i);
        }
    }
}
