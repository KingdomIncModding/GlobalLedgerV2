using BepInEx.Bootstrap;
using BepInEx.Logging;
using UnityEngine;

namespace KingdomGlobalLedgerV2
{
    internal static class GlobalLedgerEditionPriority
    {
        internal const string OriginalId = "urakka.kingdominc.globalledger";

        internal static void UnloadOriginal(ManualLogSource logger)
        {
            if (!Chainloader.PluginInfos.TryGetValue(OriginalId, out var original) || !original.Instance) return;
            original.Instance.enabled = false;
            // Plugins share BepInEx's manager GameObject. Destroy only this component,
            // synchronously, so its OnDestroy releases hooks and UI before HD registers.
            Object.DestroyImmediate(original.Instance);
            logger.LogInfo("Global Ledger HD has unloaded the original Global Ledger for this session. Its installed files are unchanged.");
        }
    }
}
