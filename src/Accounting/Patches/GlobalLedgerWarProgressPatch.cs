using KingdomFoundation.Api;
using System;
using System.Linq;

namespace KingdomGlobalLedgerV2
{
    static class GlobalLedgerWarProgressPatch
    {
        static GameWarProgress activeWar;
        static decimal warMedals;
        internal static Exception Finalizer(Exception __exception)
        {
            activeWar = null;
            warMedals = 0m;
            return __exception;
        }

        internal static void Postfix(GameWarProgress __instance)
        {
            try
            {
                if (warMedals > 0m && GlobalLedgerV2Plugin.Instance?.Book != null && FoundationApi.Kingdom != null)
                {
                    var war = __instance.War;
                    var allWarEfforts = GlobalLedgerV2Plugin.Instance.Locations.AllFlows().SelectMany(f => f.Buildings.Where(element => element.TypeId == "WarEffort")).ToList();
                    var warEfforts = allWarEfforts?.Where(w => w != null && w.Production.War == war).ToList();
                    if (warEfforts != null && warEfforts.Count > 0)
                    {
                        var connected = warEfforts.Where(w => w.Inputs != null && w.Inputs.Count > 0 && w.Inputs[0].ConnectedPipe != null).ToList();
                        var targets = connected.Count > 0 ? connected : warEfforts;
                        decimal baseShare = Math.Floor(warMedals / targets.Count);
                        decimal remainder = warMedals - baseShare * targets.Count;
                        for (int i = 0; i < targets.Count; i++)
                        {
                            decimal share = baseShare + (i < remainder ? 1m : 0m);
                            if (share > 0m)
                            {
                                var source = GlobalLedgerObserver.Describe(targets[i]);
                                GlobalLedgerV2Plugin.Instance.Book.Record(source, GameCurrency.Medal, share, 0m);
                            }
                        }
                    }
                    else
                    {
                        string warName = FoundationApi.GameInfo.Translate(war.DisplayName);
                        if (string.IsNullOrEmpty(warName) || warName.StartsWith("@"))
                            warName = war.DisplayName ?? war.Identifier;
                        var source = new GlobalLedgerBookSource("war_" + war.Identifier, "War", warName, warName, GlobalLedgerLocations.CurrentLocale, war.DisplayName);
                        GlobalLedgerV2Plugin.Instance.Book.Record(source, GameCurrency.Medal, warMedals, 0m);
                    }
                }
            }
            finally
            {
                activeWar = null;
                warMedals = 0m;
            }
        }

        internal static void Prefix(GameWarProgress __instance)
        {
            activeWar = __instance;
            warMedals = 0m;
        }

        internal static void RecordIncome(GameCurrency c, long amt)
        {
            if (activeWar != null && c == GameCurrency.Medal && amt > 0)
                warMedals = GlobalLedgerMath.AddSaturated(warMedals, amt);
        }
    }
}
