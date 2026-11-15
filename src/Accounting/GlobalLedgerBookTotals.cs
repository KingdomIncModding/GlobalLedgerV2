using System;
using System.Collections.Generic;

namespace KingdomGlobalLedgerV2
{
    internal sealed class GlobalLedgerBookTotals
    {
        internal GlobalLedgerBookTotals(GlobalLedgerBookSource source)
        {
            Source = source;
        }

        internal GlobalLedgerBookSource Source;
        decimal[] earned;
        decimal[] spent;
        Dictionary<string, decimal> itemEarned;
        Dictionary<string, decimal> itemSpent;

        internal bool Active(int currency) => EarnedAt(currency) != 0m || SpentAt(currency) != 0m;
        internal decimal EarnedAt(int currency) => earned != null ? earned[currency] : 0m;
        internal decimal SpentAt(int currency) => spent != null ? spent[currency] : 0m;
        internal decimal Net(int currency) => GlobalLedgerMath.SubSaturated(EarnedAt(currency), SpentAt(currency));

        internal bool ItemActive(string itemId) => ItemProduced(itemId) != 0m || ItemConsumed(itemId) != 0m;
        internal decimal ItemProduced(string itemId) => itemEarned != null && itemEarned.TryGetValue(itemId, out var value) ? value : 0m;
        internal decimal ItemConsumed(string itemId) => itemSpent != null && itemSpent.TryGetValue(itemId, out var value) ? value : 0m;
        internal decimal ItemNet(string itemId) => GlobalLedgerMath.SubSaturated(ItemProduced(itemId), ItemConsumed(itemId));

        internal IEnumerable<KeyValuePair<string, decimal>> EarnedItems
            => itemEarned ?? (IEnumerable<KeyValuePair<string, decimal>>)Array.Empty<KeyValuePair<string, decimal>>();

        internal IEnumerable<KeyValuePair<string, decimal>> SpentItems
            => itemSpent ?? (IEnumerable<KeyValuePair<string, decimal>>)Array.Empty<KeyValuePair<string, decimal>>();

        internal int AllocationCount
            => 1 + (earned != null ? 1 : 0) + (spent != null ? 1 : 0) + (itemEarned != null ? 1 : 0) + (itemSpent != null ? 1 : 0);

        internal void AddEarned(int currency, decimal amount)
        {
            if (amount == 0m) return;
            if (earned == null) earned = new decimal[GlobalLedgerBook.CurrencyCount];
            earned[currency] = GlobalLedgerMath.AddSaturated(earned[currency], amount);
        }

        internal void AddSpent(int currency, decimal amount)
        {
            if (amount == 0m) return;
            if (spent == null) spent = new decimal[GlobalLedgerBook.CurrencyCount];
            spent[currency] = GlobalLedgerMath.AddSaturated(spent[currency], amount);
        }

        internal void AddItemProduced(string itemId, decimal amount)
        {
            if (amount == 0m || string.IsNullOrEmpty(itemId)) return;
            if (itemEarned == null) itemEarned = new Dictionary<string, decimal>(StringComparer.Ordinal);
            itemEarned.TryGetValue(itemId, out var old);
            itemEarned[itemId] = GlobalLedgerMath.AddSaturated(old, amount);
        }

        internal void AddItemConsumed(string itemId, decimal amount)
        {
            if (amount == 0m || string.IsNullOrEmpty(itemId)) return;
            if (itemSpent == null) itemSpent = new Dictionary<string, decimal>(StringComparer.Ordinal);
            itemSpent.TryGetValue(itemId, out var old);
            itemSpent[itemId] = GlobalLedgerMath.AddSaturated(old, amount);
        }

        internal void MergeFrom(GlobalLedgerBookTotals other, long overlapSeconds = 1, long spanSeconds = 1)
        {
            Source = other.Source;
            bool partial = overlapSeconds < spanSeconds && spanSeconds > 0;
            if (other.earned != null)
            {
                for (int c = 0; c < GlobalLedgerBook.CurrencyCount; c++)
                {
                    decimal value = other.earned[c];
                    if (value == 0m) continue;
                    if (partial) value = GlobalLedgerMath.RoundWhole(value * overlapSeconds / spanSeconds);
                    AddEarned(c, value);
                }
            }
            if (other.spent != null)
            {
                for (int c = 0; c < GlobalLedgerBook.CurrencyCount; c++)
                {
                    decimal value = other.spent[c];
                    if (value == 0m) continue;
                    if (partial) value = GlobalLedgerMath.RoundWhole(value * overlapSeconds / spanSeconds);
                    AddSpent(c, value);
                }
            }
            if (other.itemEarned != null)
            {
                foreach (var item in other.itemEarned)
                {
                    decimal value = partial ? GlobalLedgerMath.RoundWhole(item.Value * overlapSeconds / spanSeconds) : item.Value;
                    AddItemProduced(item.Key, value);
                }
            }
            if (other.itemSpent != null)
            {
                foreach (var item in other.itemSpent)
                {
                    decimal value = partial ? GlobalLedgerMath.RoundWhole(item.Value * overlapSeconds / spanSeconds) : item.Value;
                    AddItemConsumed(item.Key, value);
                }
            }
        }
    }
}
