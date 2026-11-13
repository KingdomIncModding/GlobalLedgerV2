using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace KingdomGlobalLedgerV2
{
    static class GlobalLedgerConsumedPatch
    {
        static bool IsConsumedInput(WorkspaceObject element, int inputIndex, GameInventoryItem item)
        {
            if (element.Production.IsRecipe)
                return inputIndex != 0 || !element.Production.HasEnhancementResult;
            switch (element.TypeId)
            {
                case "Shop":
                case "LuxuryShop":
                case "Restaurant":
                case "Disenchanter":
                case "RequisitionOffice":
                case "NightmareCollector":
                case "Incinerator":
                case "Dungeon":
                case "WarEffort":
                    return true;
                case "Outfitter":
                case "Inn":
                case "TrainingGrounds":
                    return inputIndex == 1;
                case "Arena":
                    return item.Type?.IsHuman != true;
                default:
                    return false;
            }
        }

        internal static void Postfix(WorkspacePort __instance, GameInventoryItem __result)
        {
            if (GlobalLedgerObserver.current == null || __result?.Type == null || !__instance.IsInput || __instance.Parent != GlobalLedgerObserver.current.Element)
                return;
            var element = GlobalLedgerObserver.current.Element;
            int index = __instance.ParentIndex;
            if (element.TypeId == "TrainingGrounds" && index == 0 || element.TypeId == "QuestBoard" && index == 0 || element.TypeId == "Arena" && __result.Type.IsHuman)
            {
                if (!GlobalLedgerObserver.current.VisitorsBefore.Any(v => ReferenceEquals(v.item, __result)))
                    GlobalLedgerObserver.current.VisitorsBefore.Add((__result, __result.Type));
                return;
            }

            if (IsConsumedInput(element, index, __result))
                GlobalLedgerObserver.AddCount(GlobalLedgerObserver.current.Consumed, __result.Type.Identifier, 1);
        }
    }
}
