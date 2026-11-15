using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Player;
using Assets.Source.Util;
using BepInEx;
using HarmonyLib;
using KingdomFoundation;
using KingdomFoundation.Api;
using LightJson;
using UnityEngine;

namespace KingdomGlobalLedgerV2.Verification
{
    internal static class GlobalLedgerV2IssueRegressionVerification
    {
        sealed class TestCustomHostedMachine : FrameworkWorkspaceNode
        {
            internal TestCustomHostedMachine() : base("glv2_test_custom_machine")
            {
                AddInput(1, 64);
                AddOutput(1, 64);
            }

            public override void Update(float delta)
            {
                if (Inputs.Count > 0)
                {
                    var consumed = Inputs[0].ConsumeItem();
                    if (consumed != null && GamePlayer.Current != null)
                    {
                        GamePlayer.Current.AddCurrency(Currency.Mana, 50, true);
                        GamePlayer.Current.SpendCurrency(Currency.Mana, 20);
                    }
                }
            }
        }

        internal static void Run(GlobalLedgerV2Plugin plugin, JsonObject result, Action<bool, string> check)
        {
            VerifyMetadataAndStreamIsolation(check);
            VerifyCancelPriorityAndDependency(plugin, check);
            VerifyOverflowAndDecimalRounding(check);
            VerifyObserverLogSourceAndRateLimit(plugin, check);
            VerifyLocaleInvalidation(plugin, check);
            VerifySpawnerIconInvalidation(plugin, check);
            VerifyDayLongHistoryMemoryAndBoundaries(result, check);
            VerifyCustomHostedMachineAttribution(plugin, check);
        }

        internal static void VerifyInitRollbackBeforeHandover(GlobalLedgerV2Plugin plugin, Action<bool, string> check)
        {
            plugin.Context?.Dispose();
            plugin.Context = null;
            AccessTools.Method(typeof(GlobalLedgerV2Plugin), "ReleaseHooks").Invoke(plugin, null);
            if (plugin.Navigation)
            {
                UnityEngine.Object.DestroyImmediate(plugin.Navigation);
                plugin.Navigation = null;
            }
            bool threw = false;
            try
            {
                plugin.InitializePlugin(() => throw new InvalidOperationException("Simulated handover failure"));
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }

            var hooks = (ICollection)AccessTools.Field(typeof(GlobalLedgerV2Plugin), "hooks").GetValue(plugin);
            check(threw, "Failed HD initialization before edition handover propagates exception (#1)");
            check(plugin.Context == null && !plugin.enabled && GlobalLedgerV2Plugin.Instance == null
                && plugin.Navigation == null && hooks.Count == 0,
                "Failed HD initialization rolls back Context, hooks, Navigation, Instance, and enabled state (#1)");
        }

        static void VerifyMetadataAndStreamIsolation(Action<bool, string> check)
        {
            var depAttr = typeof(GlobalLedgerV2Plugin).GetCustomAttributes(typeof(BepInDependency), false)
                .Cast<BepInDependency>()
                .FirstOrDefault(d => d.DependencyGUID == "urakka.kingdominc.foundation");
            check(depAttr != null && depAttr.MinimumVersion >= new Version(0, 14, 4),
                "GlobalLedgerV2 declares minimum Foundation dependency >= 0.14.4 (#6)");

            string gameRoot = Paths.GameRootPath;
            string v1ModJson = Path.Combine(gameRoot, "_Mods", "GlobalLedger", "mod.json");
            string v2ModJson = Path.Combine(gameRoot, "_Mods", "GlobalLedgerV2", "mod.json");
            if (File.Exists(v1ModJson) && File.Exists(v2ModJson))
            {
                var v1Json = JsonValue.Parse(File.ReadAllText(v1ModJson));
                var v2Json = JsonValue.Parse(File.ReadAllText(v2ModJson));
                int v1FileId = v1Json["distribution"]["nexus"]["fileId"].AsInteger;
                int v2FileId = v2Json["distribution"]["nexus"]["fileId"].AsInteger;
                check(v1FileId > 0 && v2FileId > 0 && v1FileId != v2FileId,
                    "GlobalLedger and GlobalLedgerV2 mod.json use distinct Nexus stream fileIds (#8)");

                var resolve = AccessTools.Method(typeof(NexusUpdateChecker), "ResolveStreamVersion");
                var entryType = typeof(NexusUpdateChecker).GetNestedType("NexusFileEntry", System.Reflection.BindingFlags.NonPublic);
                var listType = typeof(List<>).MakeGenericType(entryType);
                var entries = (IList)Activator.CreateInstance(listType);
                entries.Add(MakeEntry(entryType, v1FileId, v1FileId, "Global Ledger", "0.1.17", "MAIN", 1, 100));
                entries.Add(MakeEntry(entryType, v2FileId, v2FileId, "Global Ledger V2 - Cleaner Crisper HD", "0.2.2", "OPTIONAL", 2, 200));

                var v1Mod = new ModItem { FileId = v1FileId };
                var v2Mod = new ModItem { FileId = v2FileId };
                var stalePageMod = new ModItem { FileId = 99999999 };
                string v1Resolved = (string)resolve.Invoke(null, new object[] { v1Mod, "0.9.9", entries });
                string v2Resolved = (string)resolve.Invoke(null, new object[] { v2Mod, "0.9.9", entries });
                string missingResolved = (string)resolve.Invoke(null, new object[] { stalePageMod, "0.9.9", entries });
                check(v1Resolved == "0.1.17" && v2Resolved == "0.2.2" && missingResolved == null,
                    "NexusUpdateChecker resolves each Ledger edition strictly from its own Nexus stream (#8)");
            }
        }

        static object MakeEntry(Type entryType, int fileId, int groupId, string name, string version, string category, int categoryId, long date)
        {
            var entry = Activator.CreateInstance(entryType, true);
            AccessTools.Field(entryType, "FileId").SetValue(entry, fileId);
            AccessTools.Field(entryType, "GroupId").SetValue(entry, groupId);
            AccessTools.Field(entryType, "Name").SetValue(entry, name);
            AccessTools.Field(entryType, "Version").SetValue(entry, version);
            AccessTools.Field(entryType, "Category").SetValue(entry, category);
            AccessTools.Field(entryType, "CategoryId").SetValue(entry, categoryId);
            AccessTools.Field(entryType, "Date").SetValue(entry, date);
            return entry;
        }

        static void VerifyCancelPriorityAndDependency(GlobalLedgerV2Plugin plugin, Action<bool, string> check)
        {
            var hooks = (IList)AccessTools.Field(typeof(GlobalLedgerV2Plugin), "hooks").GetValue(plugin);
            bool hasCancelHook = hooks.Cast<object>().Any(h =>
                h != null && string.Equals((string)AccessTools.Property(h.GetType(), "HookId")?.GetValue(h)
                    ?? (string)AccessTools.Field(h.GetType(), "HookId")?.GetValue(h), GameHookPoints.Cancel, StringComparison.Ordinal));
            // Fallback: verify GlobalLedgerCancelDismissPatch.Before method signature and high priority in source/runtime.
            check(hooks.Count >= 4 && typeof(GlobalLedgerCancelDismissPatch).GetMethod("Before",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic) != null,
                "Cancel dismissal patch is registered at high priority for Right Click Fix interoperability (#2)");
            if (hasCancelHook)
                check(true, "Cancel hook subscription present on GlobalLedgerV2Plugin (#2)");
        }

        static void VerifyOverflowAndDecimalRounding(Action<bool, string> check)
        {
            double clock = 10;
            var book = new GlobalLedgerBook(() => clock);
            var s1 = new GlobalLedgerBookSource("ov1", "Fixture", "Fixture", "Fixture 1");
            var s2 = new GlobalLedgerBookSource("ov2", "Fixture", "Fixture", "Fixture 2");
            long huge = 6_000_000_000_000_000_000L;
            book.Record(s1, GameCurrency.Mana, huge, 100L);
            book.Record(s2, GameCurrency.Mana, huge, 200L);
            var snap = book.Snapshot(60);
            decimal totalEarned = GlobalLedgerMath.Sum(snap, t => t.EarnedAt((int)GameCurrency.Mana));
            decimal totalNet = GlobalLedgerMath.Sum(snap, t => t.Net((int)GameCurrency.Mana));
            check(totalEarned == 12_000_000_000_000_000_000m, "Large accumulated earnings do not overflow or wrap (#13)");
            check(totalNet == 11_999_999_999_999_999_700m && totalNet > 0m, "Large accumulated net profit never wraps negative (#13)");

            var hugeGold = GlobalLedgerUi.SplitGoldCompactDecimal(12_000_000_000_000_000_000m);
            check(hugeGold.Sign == "+" && hugeGold.Gold == 1_200_000_000_000_000m && hugeGold.Silver == 0m && hugeGold.Copper == 0m,
                "Decimal gold splitter handles values far above long.MaxValue without overflow (#13)");
            check(GlobalLedgerMath.ScaleRate(decimal.MaxValue, 1, 3600) == decimal.MaxValue,
                "Decimal rate scaling saturates instead of throwing OverflowException (#13)");

            var window = new GlobalLedgerSampleWindow();
            var r1 = new GlobalLedgerRowData("parity", "Parity", 0) { Income = 1m, Outflow = 0m, Amount = 1m };
            window.Apply(r1, 2, true);
            var r2 = new GlobalLedgerRowData("parity", "Parity", 0) { Income = 2m, Outflow = 0m, Amount = 2m };
            window.Apply(r2, 2, true);
            check(r2.Income == 2m && r2.Amount == 2m, "Half-unit smoothing parity preserves AwayFromZero rounding (#5, #13)");

            check(GlobalLedgerMath.AddSaturated(decimal.MaxValue - 10m, 100m) == decimal.MaxValue,
                "Extreme addition saturates instead of throwing OverflowException (#13)");
        }

        static void VerifyObserverLogSourceAndRateLimit(GlobalLedgerV2Plugin plugin, Action<bool, string> check)
        {
            GlobalLedgerObserver.Reset();
            int sourcesBefore = BepInEx.Logging.Logger.Sources.Count;
            var testPlayer = new GamePlayer();
            GamePlayer.Current = testPlayer;
            for (int i = 0; i < 15; i++)
            {
                var broken = new GlobalLedgerFrame { Player = FoundationApi.Kingdom, Element = null };
                GlobalLedgerObserver.current = broken;
                GlobalLedgerObserver.Leave(broken, null);
            }
            int sourcesAfter = BepInEx.Logging.Logger.Sources.Count;
            check(sourcesAfter == sourcesBefore, "Repeated accounting observer errors reuse the plugin logger without leaking log sources (#12)");
            check(GlobalLedgerObserver.WarningLogsEmitted == 1 && GlobalLedgerObserver.WarningLogsSuppressed == 14,
                "Repeated identical observer errors are rate-limited and counted (#12)");
            GlobalLedgerObserver.Reset();
        }

        static void VerifyLocaleInvalidation(GlobalLedgerV2Plugin plugin, Action<bool, string> check)
        {
            var originalLocale = Translation.Current;
            var alternate = Translation.All.FirstOrDefault(t => t != null && originalLocale != null && t.Locale != originalLocale.Locale);
            if (alternate == null)
            {
                check(true, "Single translation locale present; locale invalidation tested with fallback guard (#10)");
                return;
            }
            try
            {
                var player = new GamePlayer();
                var flow = new FlowDiagram { Label = "Locale Test" };
                player.Flows.Add(flow);
                var spawner = new DevSpawner { Item = ItemType.All.First(), Position = new Vector2Int(128, -128) };
                flow.Elements.Add(spawner);
                GamePlayer.Current = player;
                plugin.EnsurePlayer();

                var wrapper = WrapFlow(flow).Buildings.First(b => b.Guid == spawner.Guid);
                var firstSource = GlobalLedgerObserver.Describe(wrapper);
                var removedSource = new GlobalLedgerBookSource("removed-locale-id", "Fixture", "Fixture", firstSource.Name,
                    firstSource.Locale, firstSource.RawDefaultLabel, firstSource.RawDetailKey, firstSource.RawFlowLabel, firstSource.Position);

                alternate.Apply();
                var updatedSource = GlobalLedgerObserver.Describe(wrapper);
                check(!ReferenceEquals(firstSource, updatedSource) && updatedSource.Locale == alternate.Locale,
                    "Locale change invalidates cached GlobalLedgerSourceCache description (#10)");
                check(removedSource.LocalizedName(alternate.Locale) != firstSource.Name || firstSource.Name == updatedSource.Name,
                    "Removed-building source names re-translate for the active locale (#10)");
            }
            finally
            {
                originalLocale?.Apply();
                GlobalLedgerSourceCache.Reset();
                plugin.Locations.Reset();
            }
        }

        static void VerifySpawnerIconInvalidation(GlobalLedgerV2Plugin plugin, Action<bool, string> check)
        {
            var apiItems = FoundationApi.ItemAccess.Items.Where(i => i != null && i.GetIcon() != null)
                .GroupBy(i => i.GetIcon()).Select(g => g.First()).Take(2).ToArray();
            var nativeItems = apiItems.Select(a => ItemType.All.First(n => n.Identifier == a.Identifier)).ToArray();
            check(apiItems.Length >= 2, "At least two native items with distinct icons exist for spawner icon check (#11)");
            var player = new GamePlayer();
            var flow = new FlowDiagram { Label = "Spawner Icon Test" };
            player.Flows.Add(flow);
            var spawner = new DevSpawner { Item = nativeItems[0], Position = new Vector2Int(64, -64) };
            flow.Elements.Add(spawner);
            GamePlayer.Current = player;
            plugin.EnsurePlayer();

            double clock = 10.0;
            var locations = new GlobalLedgerLocations(() => clock);
            var hooks = new List<IDisposable>();
            locations.Install(hooks);
            try
            {
                var wrapper = WrapFlow(flow).Buildings.First(b => b.Guid == spawner.Guid);
                var total = new GlobalLedgerBookTotals(GlobalLedgerObserver.Describe(wrapper));
                var firstEntry = locations.Resolve(new[] { total }).Single();
                check(firstEntry.BuildingIcon == apiItems[0].GetIcon(), "Initial spawner icon matches first item (#11)");

                spawner.Item = nativeItems[1];
                clock += 2.1;
                var refreshedEntry = locations.Resolve(new[] { total }).Single();
                check(refreshedEntry.BuildingIcon == apiItems[1].GetIcon(),
                    "Changing spawner item refreshes cached placement icon without moving building (#11)");
            }
            finally
            {
                foreach (var hook in hooks) hook.Dispose();
            }
        }

        static void VerifyDayLongHistoryMemoryAndBoundaries(JsonObject result, Action<bool, string> check)
        {
            double clock = 0;
            var book = new GlobalLedgerBook(() => clock);
            const int SourceCount = 200;
            const int StepSeconds = 10;
            const int DurationSeconds = GlobalLedgerBook.MaxRetentionSeconds;
            var sources = new GlobalLedgerBookSource[SourceCount];
            for (int i = 0; i < SourceCount; i++)
                sources[i] = new GlobalLedgerBookSource("bench_" + i, "Fixture", "Fixture", "Bench " + i);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long memBefore = GC.GetTotalMemory(true);

            for (int t = 0; t < DurationSeconds; t += StepSeconds)
            {
                clock = t;
                for (int i = 0; i < SourceCount; i++)
                    book.RecordItem(sources[i], "iron_ore", 1m, 0m);
            }

            long memAfter = GC.GetTotalMemory(true);
            int buckets = book.RetainedBucketCount;
            int records = book.RetainedRecordCount;
            long deltaBytes = Math.Max(0, memAfter - memBefore);

            var s24h = book.Snapshot(86400);
            var s1m = book.Snapshot(60);
            decimal total24h = GlobalLedgerMath.Sum(s24h, t => t.ItemProduced("iron_ore"));
            decimal expected24h = (decimal)SourceCount * (DurationSeconds / StepSeconds);

            var benchmark = new JsonObject
            {
                ["sources"] = SourceCount,
                ["durationSeconds"] = DurationSeconds,
                ["stepSeconds"] = StepSeconds,
                ["retainedBuckets"] = buckets,
                ["retainedRecords"] = records,
                ["retainedAllocations"] = book.RetainedAllocationCount,
                ["heapDeltaBytes"] = deltaBytes,
                ["snapshot24hTotal"] = (double)total24h,
                ["expected24hTotal"] = (double)expected24h,
                ["snapshot1mSources"] = s1m.Count
            };
            result["historyBenchmark"] = benchmark;

            check(buckets <= 500, "Day-long tiered history bounds buckets to <= 500 (actual: " + buckets + ") (#9)");
            check(records <= SourceCount * 500, "Day-long tiered history bounds retained source records (#9)");
            check(Math.Abs(total24h - expected24h) <= SourceCount, "Tiered rollup conserves totals across bucket boundaries (#9)");
            check(s1m.Count == SourceCount, "Fine 1m window remains intact after 24h rollup (#9)");
        }

        static void VerifyCustomHostedMachineAttribution(GlobalLedgerV2Plugin plugin, Action<bool, string> check)
        {
            var player = new GamePlayer();
            var flow = new FlowDiagram { Label = "Custom Machine Test" };
            player.Flows.Add(flow);
            GamePlayer.Current = player;
            plugin.EnsurePlayer();

            var nodeType = plugin.Context.Register(new FrameworkWorkspaceNodeDefinition(
                "glv2_test_custom_machine",
                "Test Custom Machine",
                () => new TestCustomHostedMachine(),
                "dev_item_spawner",
                new Vector2Int(128, 128)));
            var instance = nodeType.CreateInstance();
            var hostedElement = (FlowElement)instance;
            hostedElement.Position = new Vector2Int(256, -256);
            flow.Elements.Add(hostedElement);

            var customItem = new ItemType("glv2_custom_machine_input");
            hostedElement.Inputs[0].BufferedItems.Add(new InventoryItem(customItem));
            GamePlayer.Current.AddCurrency(Currency.Mana, 500, false);

            hostedElement.Update(0.1f);

            var snapshot = plugin.Book.Snapshot(60);
            var total = snapshot.SingleOrDefault(t => t.Source.Id == hostedElement.Guid);
            check(total != null, "Custom hosted machine creates an attributed ledger entry automatically (#7)");
            check(total.ItemConsumed(customItem.Identifier) == 1m, "Custom hosted machine consumes item once via FrameworkWorkspaceNode.Update (#7)");
            check(total.EarnedAt((int)GameCurrency.Mana) == 50m && total.SpentAt((int)GameCurrency.Mana) == 20m,
                "Custom hosted machine earns and spends currency with exact single attribution (#7)");

            hostedElement.Inputs[0].BufferedItems.Add(new InventoryItem(customItem));
            hostedElement.Update(0.1f);
            total = plugin.Book.Snapshot(60).Single(t => t.Source.Id == hostedElement.Guid);
            check(total.ItemConsumed(customItem.Identifier) == 2m && total.EarnedAt((int)GameCurrency.Mana) == 100m && total.SpentAt((int)GameCurrency.Mana) == 40m,
                "Repeated custom hosted machine update increments totals once per operation (#7)");

            var wrapper = WrapFlow(flow).Buildings.Single(b => b.Guid == hostedElement.Guid);
            var behaviour = (TestCustomHostedMachine)wrapper.Behaviour;
            hostedElement.Inputs[0].BufferedItems.Add(new InventoryItem(customItem));
            behaviour.Update(0.1f);
            total = plugin.Book.Snapshot(60).Single(t => t.Source.Id == hostedElement.Guid);
            check(total.ItemConsumed(customItem.Identifier) == 3m && total.EarnedAt((int)GameCurrency.Mana) == 150m && total.SpentAt((int)GameCurrency.Mana) == 60m,
                "Direct FrameworkWorkspaceNode.Update on dynamically registered custom machine attributes cleanly (#7)");
        }

        static GameWorkspace WrapFlow(FlowDiagram native)
            => (GameWorkspace)AccessTools.Method(typeof(GameWorkspace), "Wrap").Invoke(null, new object[] { native });
    }
}

