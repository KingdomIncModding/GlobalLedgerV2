using System;
using System.Collections.Generic;

namespace KingdomGlobalLedgerV2
{
    internal sealed class GlobalLedgerSampleWindow
    {
        readonly Queue<(decimal Income, decimal Outflow)> values = new Queue<(decimal, decimal)>();
        decimal income, outflow;

        internal void Apply(GlobalLedgerRowData row, int count, bool sample)
        {
            if (sample || values.Count == 0)
            {
                values.Enqueue((row.Income, row.Outflow));
                income = GlobalLedgerMath.AddSaturated(income, row.Income);
                outflow = GlobalLedgerMath.AddSaturated(outflow, row.Outflow);
            }
            while (values.Count > Math.Max(1, count))
            {
                var previous = values.Dequeue();
                income = GlobalLedgerMath.SubSaturated(income, previous.Income);
                outflow = GlobalLedgerMath.SubSaturated(outflow, previous.Outflow);
            }
            row.Income = GlobalLedgerMath.RoundWhole(income / values.Count);
            row.Outflow = GlobalLedgerMath.RoundWhole(outflow / values.Count);
            row.Amount = GlobalLedgerMath.SubSaturated(row.Income, row.Outflow);
        }
    }
}

