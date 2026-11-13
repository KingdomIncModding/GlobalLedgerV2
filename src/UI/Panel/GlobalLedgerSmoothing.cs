using System.Collections.Generic;
using System.Linq;

namespace KingdomGlobalLedgerV2
{
    internal sealed class GlobalLedgerSmoothing
    {
        readonly Dictionary<string, Queue<(long Income, long Outflow)>> history = new Dictionary<string, Queue<(long, long)>>();
        internal void Clear() => history.Clear();
        internal void Apply(IEnumerable<GlobalLedgerRowData> rows, int samples, bool sample)
        {
            foreach (var row in rows)
            {
                if (row.IsHeader || (!row.GameCurrency.HasValue && row.Item == null)) continue;
                if (!history.TryGetValue(row.Key, out var values)) history[row.Key] = values = new Queue<(long, long)>();
                if (sample || values.Count == 0) values.Enqueue((row.Income, row.Outflow));
                while (values.Count > samples) values.Dequeue();
                row.Income = (long)System.Math.Round(values.Average(value => value.Income));
                row.Outflow = (long)System.Math.Round(values.Average(value => value.Outflow));
                row.Amount = row.Income - row.Outflow;
            }
        }
    }
}
