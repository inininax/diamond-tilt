using System;

namespace DiamondTilt.Core.Economy
{
    public enum ReconcileStatus
    {
        NoLedger,
        Reconciled,
        InvalidChain
    }

    public static class WalletReconciliation
    {
        public static ReconcileStatus Apply(SaveData data, byte[] key)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            if (data.Ledger == null || data.Ledger.Count == 0) return ReconcileStatus.NoLedger;
            if (!Wallet.VerifyChain(data.Ledger, key)) return ReconcileStatus.InvalidChain;

            long coins = 0, gems = 0;
            foreach (var e in data.Ledger)
            {
                if (e == null) continue;
                if ((CurrencyType)e.Currency == CurrencyType.Coins) coins = e.BalanceAfter;
                else gems = e.BalanceAfter;
            }

            data.WalletCoins = coins;
            data.WalletGems = gems;
            return ReconcileStatus.Reconciled;
        }
    }
}
