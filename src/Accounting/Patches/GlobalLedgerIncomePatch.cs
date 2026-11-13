using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace KingdomGlobalLedgerV2
{
    static class GlobalLedgerIncomePatch
    {
        internal static void Postfix(GameKingdom __instance, GameCurrency c, long amt, bool isIncome)
        {
            if (!isIncome || amt <= 0)
                return;
            if (GlobalLedgerObserver.current != null && GlobalLedgerObserver.current.Player == __instance)
                GlobalLedgerObserver.current.Income[(int)c] += amt;
            else if (GlobalLedgerObserver.current == null)
                GlobalLedgerWarProgressPatch.RecordIncome(c, amt);
        }
    }
}
