using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace KingdomGlobalLedgerV2
{
    static class GlobalLedgerSpendingPatch
    {
        internal static void Postfix(GameKingdom __instance, GameCurrency c, long amt, bool __result)
        {
            if (__result && amt > 0 && GlobalLedgerObserver.current != null && GlobalLedgerObserver.current.Player == __instance)
                GlobalLedgerObserver.current.Spending[(int)c] += amt;
        }
    }
}
