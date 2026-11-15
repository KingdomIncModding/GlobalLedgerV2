using System;
using System.Collections.Generic;

namespace KingdomGlobalLedgerV2
{
    /// <summary>Overflow-safe decimal arithmetic helpers for accounting totals and rates.</summary>
    internal static class GlobalLedgerMath
    {
        internal static decimal AddSaturated(decimal a, decimal b)
        {
            try
            {
                return checked(a + b);
            }
            catch (OverflowException)
            {
                return b >= 0m ? decimal.MaxValue : decimal.MinValue;
            }
        }

        internal static decimal SubSaturated(decimal a, decimal b)
        {
            try
            {
                return checked(a - b);
            }
            catch (OverflowException)
            {
                return a >= 0m ? decimal.MaxValue : decimal.MinValue;
            }
        }

        internal static decimal RoundWhole(decimal value) => Math.Round(value, 0, MidpointRounding.AwayFromZero);

        internal static decimal Sum<T>(IEnumerable<T> source, Func<T, decimal> selector)
        {
            decimal total = 0m;
            foreach (var item in source)
                total = AddSaturated(total, selector(item));
            return total;
        }

        internal static double Rate(decimal value, int windowSeconds, int unitSeconds)
        {
            if (windowSeconds <= 0)
                windowSeconds = 1;
            return (double)value / windowSeconds * unitSeconds;
        }

        internal static decimal ScaleRate(decimal value, int windowSeconds, int unitSeconds)
        {
            if (windowSeconds <= 0)
                windowSeconds = 1;
            try
            {
                return checked(value / windowSeconds * unitSeconds);
            }
            catch (OverflowException)
            {
                return value >= 0m ? decimal.MaxValue : decimal.MinValue;
            }
        }
    }
}
