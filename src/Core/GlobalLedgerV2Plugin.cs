using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomGlobalLedgerV2
{
    [BepInPlugin(Id, "Global Ledger V2", Version)]
    [BepInDependency("urakka.kingdominc.foundation", "0.14.2")]
    public sealed class GlobalLedgerV2Plugin : BaseUnityPlugin
    {
        void Awake()
        {
            Instance = this;
            try
            {
                Context = FoundationApi.ForMod(Id);
                EnableRollingAverage = Config.Bind("Averages", "EnableRollingAverage", false, "Checking this box enables Rolling average .. makes the calculations less jumpy but not as accurate realtime.");
                RollingAverageEntries = Config.Bind("Averages", "RollingAverageEntries", 3, "Number of entries to average when Rolling average is enabled.");
                RegisterModConfig();
                GlobalLedgerObserver.Install(hooks);
                Locations.Install(hooks);
                hooks.Add(FoundationApi.GameHooks.Subscribe(Id, GameHookPoints.UiVisibility, after: call => GlobalLedgerHudVisibilityPatch.Postfix(call.View)));
                hooks.Add(FoundationApi.GameHooks.Subscribe(Id, GameHookPoints.Escape, before: call =>
                {
                    if (!GlobalLedgerEscapeDismissPatch.Prefix(call.View))
                        call.Cancel();
                }));
                Navigation = gameObject.AddComponent<GlobalLedgerNavigation>();
                Context.Register(new GlobalLedgerBarElement(this));
            }
            catch
            {
                Context?.Dispose();
                ReleaseHooks();
                Instance = null;
                throw;
            }
        }

        public const string Id = "urakka.kingdominc.globalledgerv2", Version = "0.2.0";
        internal static GlobalLedgerV2Plugin Instance;
        internal readonly GlobalLedgerBook Book = new GlobalLedgerBook(() => Time.realtimeSinceStartupAsDouble);
        internal readonly GlobalLedgerLocations Locations = new GlobalLedgerLocations();
        internal GlobalLedgerPanel Panel;
        internal GlobalLedgerNavigation Navigation;
        internal FoundationModContext Context;
        internal ConfigEntry<bool> EnableRollingAverage;
        internal ConfigEntry<int> RollingAverageEntries;
        System.Collections.Generic.List<IDisposable> hooks = new System.Collections.Generic.List<IDisposable>();
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
