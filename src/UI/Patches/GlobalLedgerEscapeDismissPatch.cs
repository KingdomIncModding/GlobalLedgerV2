using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomGlobalLedgerV2
{
    static class GlobalLedgerEscapeDismissPatch
    {
        internal static bool Prefix(Component __instance)
        {
            var panel = GlobalLedgerV2Plugin.Instance?.Panel;
            if (!panel || !panel.gameObject.activeSelf || !__instance.GetComponent<Canvas>().enabled || FoundationApi.UiAccess.IsAlertVisible || FoundationApi.UiAccess.HasPopup)
                return true;
            panel.ClosePopup();
            return false;
        }
    }
}
