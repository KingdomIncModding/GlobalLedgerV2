using KingdomFoundation.Api;

namespace KingdomGlobalLedgerV2
{
    static class GlobalLedgerIncomePatch
    {
        internal static void Postfix(GameKingdom __instance, GameCurrency c, long amt, bool isIncome)
        {
            if (!isIncome || amt <= 0)
                return;
            if (GlobalLedgerObserver.current != null && GlobalLedgerObserver.current.Player == __instance)
                GlobalLedgerObserver.current.Income[(int)c] = GlobalLedgerMath.AddSaturated(GlobalLedgerObserver.current.Income[(int)c], amt);
            else if (GlobalLedgerObserver.current == null)
                GlobalLedgerWarProgressPatch.RecordIncome(c, amt);
        }
    }
}
