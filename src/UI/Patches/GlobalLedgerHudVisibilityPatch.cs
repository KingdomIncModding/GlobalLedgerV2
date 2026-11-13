using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomGlobalLedgerV2
{
    static class GlobalLedgerHudVisibilityPatch
    {
        internal static void Postfix(Component __instance)
        {
            var panel = GlobalLedgerV2Plugin.Instance?.Panel;
            if (panel && panel.gameObject.activeSelf && !__instance.GetComponent<Canvas>().enabled)
                panel.ClosePopup();
        }
    }
}
