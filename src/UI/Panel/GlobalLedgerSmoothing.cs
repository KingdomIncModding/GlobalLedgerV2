using System.Collections.Generic;

namespace KingdomGlobalLedgerV2
{
    internal sealed class GlobalLedgerSmoothing
    {
        readonly Dictionary<string, GlobalLedgerSampleWindow> history = new Dictionary<string, GlobalLedgerSampleWindow>();
        internal void Clear() => history.Clear();

        internal void Apply(IEnumerable<GlobalLedgerRowData> rows, int samples, bool sample)
        {
            foreach (var row in rows)
            {
                if (row.IsHeader || (!row.GameCurrency.HasValue && row.Item == null)) continue;
                if (!history.TryGetValue(row.Key, out var window))
                    history[row.Key] = window = new GlobalLedgerSampleWindow();
                window.Apply(row, samples, sample);
            }
        }
    }
}
