using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using KingdomFoundation.Api;

namespace KingdomGlobalLedgerV2
{
    [BepInPlugin(Id, "Global Ledger V2", Version)]
    [BepInDependency("urakka.kingdominc.foundation", "0.14.4")]
    [BepInDependency(GlobalLedgerEditionPriority.OriginalId, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class GlobalLedgerV2Plugin : BaseUnityPlugin
    {
        void Awake() => InitializePlugin(null);

        internal void InitializePlugin(Action beforeHandover)
        {
            Instance = this;
            try
            {
                Context = FoundationApi.ForMod(Id);
                EnableRollingAverage = Config.Bind("Averages", "EnableRollingAverage", false, "Checking this box enables Rolling average .. makes the calculations less jumpy but not as accurate realtime.");
                RollingAverageEntries = Config.Bind("Averages", "RollingAverageEntries", 3, "Number of entries to average when Rolling average is enabled.");
                RegisterModConfig();
                using (var assets = new GlobalLedgerDesignAssets())
                    if (assets.Get("DecoTitleRed") == null)
                        throw new InvalidOperationException("Missing Global Ledger HD UI asset DecoTitleRed.");
                GlobalLedgerObserver.Install(hooks);
                Locations.Install(hooks);
                hooks.Add(FoundationApi.GameHooks.Subscribe(Id, GameHookPoints.Cancel, before: GlobalLedgerCancelDismissPatch.Before, priority: 100));
                hooks.Add(FoundationApi.GameHooks.Subscribe(Id, GameHookPoints.UiVisibility, after: call => GlobalLedgerHudVisibilityPatch.Postfix(call.View)));
                hooks.Add(FoundationApi.GameHooks.Subscribe(Id, GameHookPoints.Escape, before: call =>
                {
                    if (!GlobalLedgerEscapeDismissPatch.Prefix(call.View))
                        call.Cancel();
                }));
                Navigation = gameObject.AddComponent<GlobalLedgerNavigation>();
                Context.Register(new GlobalLedgerBarElement(this));
                beforeHandover?.Invoke();
                // Hand over from original edition only after HD initialization and Mod Bar registration succeed.
                GlobalLedgerEditionPriority.UnloadOriginal(Logger);
            }
            catch
            {
                Context?.Dispose();
                Context = null;
                ReleaseHooks();
                if (Navigation)
                {
                    Destroy(Navigation);
                    Navigation = null;
                }
                if (Instance == this)
                    Instance = null;
                enabled = false;
                throw;
            }
        }

        public const string Id = "urakka.kingdominc.globalledgerv2", Version = "0.2.2";
        internal static GlobalLedgerV2Plugin Instance;
        internal readonly GlobalLedgerBook Book = new GlobalLedgerBook(() => UnityEngine.Time.realtimeSinceStartupAsDouble);
        internal readonly GlobalLedgerLocations Locations = new GlobalLedgerLocations();
        internal GlobalLedgerPanel Panel;
        internal GlobalLedgerNavigation Navigation;
        internal FoundationModContext Context;
        static ManualLogSource fallbackLogger;
        internal static void LogWarningMessage(string message)
        {
            if (Instance != null)
            {
                Instance.Logger.LogWarning(message);
                return;
            }
            if (fallbackLogger == null)
                fallbackLogger = BepInEx.Logging.Logger.CreateLogSource("Global Ledger V2");
            fallbackLogger.LogWarning(message);
        }

        internal ConfigEntry<bool> EnableRollingAverage;
        internal ConfigEntry<int> RollingAverageEntries;
        readonly List<IDisposable> hooks = new List<IDisposable>();
        GameKingdom player;
        void ReleaseHooks()
        {
            foreach (var hook in hooks)
                hook.Dispose();
            hooks.Clear();
        }

        internal void EnsurePlayer()
        {
            if (ReferenceEquals(player, FoundationApi.Kingdom))
                return;
            player = FoundationApi.Kingdom;
            Book.Reset();
            GlobalLedgerObserver.Reset();
            Locations.Reset();
            if (Navigation)
                Navigation.Clear();
            if (Panel)
            {
                Panel.ResetTree();
                if (Panel.gameObject.activeSelf)
                    Panel.ClosePopup();
            }
        }

        void OnDestroy()
        {
            Context?.Dispose();
            Context = null;
            ReleaseHooks();
            GlobalLedgerObserver.Reset();
            if (Navigation)
                Destroy(Navigation);
            if (Instance == this)
                Instance = null;
        }

        void RegisterModConfig()
        {
            var section = Context.Settings("Global Ledger V2", "Tracks live currency earnings, operating expenses, and item production across your entire kingdom.");
            section.ClearElements();
            section.AddToggle(EnableRollingAverage, "Rolling average");
            section.AddText("Checking this box enables rolling averages, making the calculations less jumpy but not as accurate in real time.");
            section.AddSlider(RollingAverageEntries, 2, 30, "Entry Count");
        }

        void Update() => EnsurePlayer();
    }
}
