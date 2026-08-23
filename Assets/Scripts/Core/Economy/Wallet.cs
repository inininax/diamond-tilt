using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace DiamondTilt.Core.Economy
{
    public enum CurrencyType
    {
        Coins = 0,
        Gems = 1
    }

    public enum LedgerEntryType
    {
        Grant = 0,
        Spend = 1
    }

    public sealed class EconomyException : Exception
    {
        public EconomyException(string message) : base(message)
        {
        }
    }

    [Serializable]
    public sealed class LedgerEntry
    {
        public long Seq;
        public int Type;
        public int Currency;
        public long Amount;
        public long BalanceAfter;
        public string Reason = "";
        public string DayKey = "";
        public string PrevHash = "";
        public string Hash = "";
    }

    public sealed class Wallet
    {
        private const long MaxBalance = 1_000_000_000_000;

        private readonly byte[] _key;
        private readonly List<LedgerEntry> _entries = new List<LedgerEntry>();
        private readonly EconomyEventBus _bus;

        public long Coins { get; private set; }
        public long Gems { get; private set; }
        public IReadOnlyList<LedgerEntry> Entries => _entries;

        public Wallet(byte[] integrityKey, long coins = 0, long gems = 0, EconomyEventBus bus = null)
        {
            if (integrityKey == null || integrityKey.Length < 16) throw new ArgumentException("key required", nameof(integrityKey));
            if (coins < 0 || gems < 0) throw new EconomyException("initial balances cannot be negative");

            _key = integrityKey;
            _bus = bus;
            Coins = Math.Min(coins, MaxBalance);
            Gems = Math.Min(gems, MaxBalance);
        }

        public void Grant(CurrencyType currency, long amount, string reason, IClock clock)
            => Mutate(LedgerEntryType.Grant, currency, amount, reason, clock);

        public void Spend(CurrencyType currency, long amount, string reason, IClock clock)
            => Mutate(LedgerEntryType.Spend, currency, amount, reason, clock);

        public long BalanceOf(CurrencyType currency)
            => currency == CurrencyType.Coins ? Coins : Gems;

        public static bool VerifyChain(IReadOnlyList<LedgerEntry> entries, byte[] key)
        {
            if (entries == null || key == null || key.Length < 16) return false;
            string prevHash = "0";
            long expectedSeq = 0;
            foreach (var e in entries)
            {
                if (e.Seq != expectedSeq++) return false;
                if (e.PrevHash != prevHash) return false;
                if (ComputeHash(e, key) != e.Hash) return false;
                prevHash = e.Hash;
            }
            return true;
        }

        public static Wallet FromEntries(byte[] key, IReadOnlyList<LedgerEntry> entries)
        {
            if (!VerifyChain(entries, key)) throw new EconomyException("ledger verification failed");
            long coins = 0, gems = 0;
            foreach (var e in entries)
            {
                var c = (CurrencyType)e.Currency;
                if (c == CurrencyType.Coins) coins = e.BalanceAfter;
                else gems = e.BalanceAfter;
            }
            var wallet = new Wallet(key, coins, gems);
            wallet._entries.AddRange(entries);
            return wallet;
        }

        private void Mutate(LedgerEntryType type, CurrencyType currency, long amount, string reason, IClock clock)
        {
            if (amount <= 0) throw new EconomyException("amount must be positive");
            if (string.IsNullOrEmpty(reason)) throw new EconomyException("reason required");
            if (clock == null) throw new ArgumentNullException(nameof(clock));

            long balance = BalanceOf(currency);
            long applied = amount;

            if (type == LedgerEntryType.Spend)
            {
                if (amount > balance) throw new EconomyException($"insufficient {currency}");
                applied = -amount;
            }
            else
            {
                applied = Math.Min(amount, MaxBalance - balance);
                if (applied <= 0) return;
            }

            long next = balance + applied;

            var entry = new LedgerEntry
            {
                Seq = _entries.Count,
                Type = (int)type,
                Currency = (int)currency,
                Amount = Math.Abs(applied),
                BalanceAfter = next,
                Reason = reason,
                DayKey = TimeKeys.DayKey(clock.UtcNow),
                PrevHash = LastHash(),
            };
            entry.Hash = ComputeHash(entry, _key);

            SetBalance(currency, next);
            _entries.Add(entry);
            _bus?.Publish(
                type == LedgerEntryType.Grant ? EconomyEventType.BalanceGranted : EconomyEventType.BalanceSpent,
                $"{currency}:{reason}");
        }

        private void SetBalance(CurrencyType currency, long value)
        {
            if (currency == CurrencyType.Coins) Coins = value;
            else Gems = value;
        }

        private string LastHash() => _entries.Count == 0 ? "0" : _entries[_entries.Count - 1].Hash;

        internal static string ComputeHash(LedgerEntry e, byte[] key)
        {
            using var hmac = new HMACSHA256(key);
            byte[] data = Encoding.UTF8.GetBytes(
                $"{e.Seq}|{e.Type}|{e.Currency}|{e.Amount}|{e.BalanceAfter}|{e.Reason}|{e.DayKey}|{e.PrevHash}");
            byte[] hash = hmac.ComputeHash(data);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
