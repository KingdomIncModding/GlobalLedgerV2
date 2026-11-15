using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomGlobalLedgerV2
{
    internal sealed class GlobalLedgerLocations
    {
        internal static string CurrentLocale => FoundationApi.Presentation?.CurrentLocale ?? CultureInfo.CurrentUICulture.Name;
        internal static string CleanLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            string cleaned = RichTextTags.Replace(value, "").Trim();
            return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
        }

        internal static Sprite ContainerIcon()
        {
            if (!cachedContainerIcon)
                cachedContainerIcon = FoundationApi.Decorations.ContainerIcon;
            return cachedContainerIcon;
        }

        static readonly Regex RichTextTags = new Regex(@"</?[a-zA-Z][^>]*>", RegexOptions.Compiled);
        static Sprite cachedContainerIcon;
        readonly Dictionary<WorkspaceObject, string> containerIds = new Dictionary<WorkspaceObject, string>();
        readonly Dictionary<string, GlobalLedgerLocationEntry> placements = new Dictionary<string, GlobalLedgerLocationEntry>();
        readonly Dictionary<WorkspaceObject, GameWorkspace> parents = new Dictionary<WorkspaceObject, GameWorkspace>();
        readonly Func<double> clock;
        GameKingdom indexedPlayer;
        IReadOnlyList<GameWorkspace> indexedFlows = Array.Empty<GameWorkspace>();
        bool dirty = true;
        ulong signature;
        double nextReconcile;
        internal int DiscoveryCount { get; private set; }
        internal int PlacementBuildCount { get; private set; }
        internal int CachedPlacementCount => placements.Count;

        internal GlobalLedgerLocations(Func<double> clock = null)
        {
            this.clock = clock ?? (() => Time.realtimeSinceStartupAsDouble);
        }

        internal void Install(List<IDisposable> hooks)
        {
            // Existing Foundation notifications only; no reflection, native types or API changes.
            hooks.Add(FoundationApi.Events.Subscribe<WorkspaceChangedEvent>(GlobalLedgerV2Plugin.Id, change => Invalidate()));
            hooks.Add(FoundationApi.Events.Subscribe<KingdomFlowCreatedEvent>(GlobalLedgerV2Plugin.Id, change => Invalidate()));
            hooks.Add(FoundationApi.Events.Subscribe<MachineRecipeChangedEvent>(GlobalLedgerV2Plugin.Id, change => Invalidate()));
            hooks.Add(FoundationApi.ExecutionHooks.Subscribe(GlobalLedgerV2Plugin.Id, GameExecutionPoints.WorkspaceEdit,
                call => null, (call, state) => Invalidate()));
            hooks.Add(FoundationApi.GameHooks.Subscribe(GlobalLedgerV2Plugin.Id, GameHookPoints.DecorationStyleUpdate,
                after: call => Invalidate()));
            hooks.Add(FoundationApi.GameHooks.Subscribe(GlobalLedgerV2Plugin.Id, GameHookPoints.BuildingClearPorts,
                after: call => Invalidate()));
            hooks.Add(FoundationApi.GameHooks.Subscribe(GlobalLedgerV2Plugin.Id, GameHookPoints.SpawnerIcon,
                after: call => Invalidate()));
        }

        internal void Invalidate() => dirty = true;

        internal IReadOnlyList<GameWorkspace> AllFlows()
        {
            Reconcile();
            return indexedFlows;
        }

        internal GameWorkspace WorkspaceOf(WorkspaceObject building)
        {
            Reconcile();
            return parents.TryGetValue(building, out var flow) ? flow : building.ParentWorkspace;
        }

        void Reconcile()
        {
            var player = FoundationApi.Kingdom;
            double now = clock();
            if (!dirty && indexedPlayer == player && now < nextReconcile)
                return;
            var flows = GetAllFlows(player).ToArray();
            ulong nextSignature = Fingerprint(flows);
            DiscoveryCount++;
            if (dirty || indexedPlayer != player || nextSignature != signature)
            {
                indexedFlows = flows;
                placements.Clear();
                parents.Clear();
                foreach (var flow in flows)
                    foreach (var building in flow.Buildings)
                    {
                        parents[building] = flow;
                        GlobalLedgerCustomNodeHooks.ObserveBuilding(building);
                    }
                foreach (var stale in containerIds.Keys.Where(container => !flows.Any(flow => flow.Decorations.Contains(container))).ToArray())
                    containerIds.Remove(stale);
            }
            indexedPlayer = player;
            signature = nextSignature;
            dirty = false;
            // Catch direct model edits from other mods that do not publish workspace events.
            nextReconcile = now + 2.0;
        }

        static ulong Fingerprint(IReadOnlyList<GameWorkspace> flows)
        {
            unchecked
            {
                ulong value = 14695981039346656037UL;
                Mix(ref value, CurrentLocale);
                foreach (var flow in flows)
                {
                    Mix(ref value, flow.GetHashCode());
                    Mix(ref value, flow.Label);
                    Mix(ref value, flow.BuildingCount);
                    foreach (var building in flow.Buildings)
                    {
                        Mix(ref value, building.GetHashCode());
                        Mix(ref value, building.Label);
                        Mix(ref value, building.Position.GetHashCode());
                        Mix(ref value, building.Size.GetHashCode());
                        Mix(ref value, building.IconName);
                        Sprite worldIcon = building.World ? FoundationApi.WorkspaceVisuals.Icon(building.World)?.sprite : null;
                        Mix(ref value, worldIcon ? worldIcon.GetInstanceID() : 0);
                        Mix(ref value, building.Production.RecipeDisplayName);
                        Mix(ref value, building.SpawnedItem?.Identifier);
                    }
                    Mix(ref value, flow.DecorationCount);
                    foreach (var deco in flow.Decorations)
                    {
                        Mix(ref value, deco.GetHashCode());
                        Mix(ref value, deco.Label);
                        Mix(ref value, deco.Position.GetHashCode());
                        Mix(ref value, deco.Size.GetHashCode());
                        Mix(ref value, deco.IsContainer ? 1 : 0);
                        Mix(ref value, deco.StickerIconName);
                    }
                }
                return value;
            }
        }

        static void Mix(ref ulong value, int next)
        {
            unchecked { value = (value ^ (uint)next) * 1099511628211UL; }
        }

        static void Mix(ref ulong value, string next)
        {
            Mix(ref value, next?.Length ?? -1);
            if (next != null)
                foreach (char c in next)
                    Mix(ref value, c);
        }

        int nextId;
        static Sprite LiveBuildingIcon(WorkspaceObject building)
        {
            if (building == null)
                return null;
            Sprite worldSprite = building.World ? FoundationApi.WorkspaceVisuals.Icon(building.World)?.sprite : null;
            if (worldSprite)
                return worldSprite;
            if (building.IsSpawner && building.SpawnedItem != null && !string.IsNullOrEmpty(building.SpawnedItem.IconName))
            {
                try
                {
                    Sprite itemSprite = building.SpawnedItem.GetIcon();
                    if (itemSprite)
                        return itemSprite;
                }
                catch { }
            }
            return building.Icon;
        }

        static Sprite DefaultIcon(WorkspaceObject container)
        {
            if (container?.World)
            {
                // Native frames don't expose an icon; use their own background sprite.
                var image = FoundationApi.Decorations.Background(container.World);
                if (image && image.sprite)
                    return image.sprite;
            }

            return FoundationApi.GameInfo.Icon("Icons2_45");
        }

        internal static IEnumerable<GameWorkspace> GetAllFlows(GameKingdom player)
        {
            if (player?.Workspaces == null)
                yield break;
            var visited = new HashSet<GameWorkspace>();
            foreach (var flow in player.Workspaces)
            {
                if (flow == null || !visited.Add(flow))
                    continue;
                yield return flow;
                foreach (var sub in GetSubFlows(flow, visited))
                    yield return sub;
            }
        }

        private static IEnumerable<GameWorkspace> GetSubFlows(GameWorkspace flow, HashSet<GameWorkspace> visited)
        {
            if (flow?.Buildings == null)
                yield break;
            for (int i = 0; i < flow.Buildings.Count; i++)
            {
                var el = flow.Buildings[i];
                if (el == null)
                    continue;
                var inner = FoundationApi.WorkspaceAccess.ExtensionWorkspace(el, "InnerFlow");
                if (inner != null && visited.Add(inner))
                {
                    yield return inner;
                    foreach (var sub in GetSubFlows(inner, visited))
                        yield return sub;
                }
            }
        }

        internal static bool Overlap(WorkspaceObject container, Vector2Int position, Vector2Int size)
        {
            return container.Position.x < (long)position.x + size.x && (long)container.Position.x + container.Size.x > position.x && container.Position.y > (long)position.y - size.y && (long)container.Position.y - container.Size.y < position.y;
        }

        internal void Reset()
        {
            containerIds.Clear();
            nextId = 0;
            placements.Clear();
            parents.Clear();
            indexedFlows = Array.Empty<GameWorkspace>();
            indexedPlayer = null;
            dirty = true;
            nextReconcile = 0;
            DiscoveryCount = PlacementBuildCount = 0;
        }

        internal IReadOnlyList<GlobalLedgerLocationEntry> Resolve(IReadOnlyList<GlobalLedgerBookTotals> totals)
        {
            Reconcile();
            if (indexedPlayer == null)
                return Array.Empty<GlobalLedgerLocationEntry>();
            var missing = totals.Where(item => !placements.ContainsKey(item.Source.Id)).ToArray();
            if (missing.Length != 0)
            {
                foreach (var entry in ResolveUncached(missing))
                    placements[entry.Totals.Source.Id] = entry;
                PlacementBuildCount++;
            }
            var result = new List<GlobalLedgerLocationEntry>(totals.Count);
            string locale = CurrentLocale;
            foreach (var item in totals)
            {
                var entry = placements[item.Source.Id];
                entry.Totals = item;
                if (entry.BuildingTarget == null)
                    entry.BuildingName = item.Source.LocalizedName(locale);
                result.Add(entry);
            }
            return result;
        }

        IReadOnlyList<GlobalLedgerLocationEntry> ResolveUncached(IReadOnlyList<GlobalLedgerBookTotals> totals)
        {
            var player = FoundationApi.Kingdom;
            if (player == null)
                return Array.Empty<GlobalLedgerLocationEntry>();
            var allFlows = indexedFlows.ToList();
            var live = new Dictionary<string, GlobalLedgerTarget>();
            foreach (var flow in allFlows)
                foreach (var element in flow.Buildings)
                    live[element.Guid] = new GlobalLedgerTarget(flow, element);
            // Pre-scan all live containers in a fixed spatial order across workspaces:
            // flow index -> position Y descending -> position X ascending -> decoration index.
            var containerList = new List<(GameWorkspace Flow, WorkspaceObject Deco, string BaseName)>();
            for (int f = 0; f < allFlows.Count; f++)
            {
                var flow = allFlows[f];
                if (flow?.Decorations == null)
                    continue;
                for (int d = 0; d < flow.Decorations.Count; d++)
                {
                    var deco = flow.Decorations[d];
                    if (deco != null && deco.IsContainer)
                    {
                        var labels = flow.Decorations.Where(l => l.Label != null && CleanLabel(l.Label) != null && Overlap(deco, l.Position, Vector2Int.RoundToInt(l.Size)));
                        var label = labels.OrderBy(l => (l.Position - deco.Position).sqrMagnitude).FirstOrDefault();
                        string baseName = (label != null ? CleanLabel(label.Label) : null) ?? "Container [" + deco.Position.x + ", " + deco.Position.y + "]";
                        containerList.Add((flow, deco, baseName));
                    }
                }
            }

            var sortedContainers = containerList.OrderBy(c => allFlows.IndexOf(c.Flow)).ThenByDescending(c => c.Deco.Position.y).ThenBy(c => c.Deco.Position.x).ThenBy(c => c.Flow.Decorations.ToList().IndexOf(c.Deco)).ToList();
            var containerNames = new Dictionary<WorkspaceObject, string>();
            foreach (var group in sortedContainers.GroupBy(c => c.BaseName, StringComparer.Ordinal))
            {
                var items = group.ToList();
                if (items.Count > 1)
                {
                    for (int i = 0; i < items.Count; i++)
                        containerNames[items[i].Deco] = items[i].BaseName + "_" + (i + 1);
                }
                else
                {
                    containerNames[items[0].Deco] = items[0].BaseName;
                }
            }

            foreach (var item in sortedContainers)
            {
                if (!containerIds.TryGetValue(item.Deco, out _))
                    containerIds[item.Deco] = "container/" + ++nextId;
            }

            var result = new List<GlobalLedgerLocationEntry>();
            foreach (var item in totals)
            {
                var entry = new GlobalLedgerLocationEntry
                {
                    Totals = item,
                    BuildingName = item.Source.LocalizedName(CurrentLocale)
                };
                if (!live.TryGetValue(item.Source.Id, out var target))
                {
                    if (item.Source.TypeId == "War")
                    {
                        entry.GroupKey = "war";
                        entry.GroupName = "War Progress";
                        var war = FoundationApi.GameInfo.FindWar(item.Source.Id.StartsWith("war_") ? item.Source.Id.Substring(4) : item.Source.Id);
                        entry.GroupIcon = entry.BuildingIcon = war?.Icon ?? DefaultIcon(null);
                        result.Add(entry);
                        continue;
                    }

                    entry.GroupKey = "removed";
                    entry.GroupName = "Removed buildings";
                    entry.GroupIcon = DefaultIcon(null);
                    result.Add(entry);
                    continue;
                }

                var flow = target.Flow;
                var building = target.Building;
                entry.BuildingTarget = target;
                entry.BuildingName = FoundationApi.GameInfo.Translate(building.Label) + " [" + building.Position.x + ", " + building.Position.y + "]";
                entry.BuildingIcon = LiveBuildingIcon(building);
                // A machine overlapping nested/overlapping frames belongs once to the
                // smallest frame. This matches native overlap-based container grouping
                // while avoiding double-counted net totals in overlapping containers.
                var buildingSize = building.World ? Vector2Int.RoundToInt(FoundationApi.WorkspaceVisuals.Size(building.World)) : new Vector2Int(Math.Max(128, Mathf.RoundToInt(building.Size.x)), Math.Max(128, Mathf.RoundToInt(building.Size.y)));
                var container = flow.Decorations.Where(d => d.IsContainer && Overlap(d, building.Position, buildingSize)).OrderBy(d => (long)d.Size.x * d.Size.y).FirstOrDefault();
                if (container == null)
                {
                    int flowIdx = allFlows.IndexOf(flow);
                    entry.GroupKey = "uncontained/" + (flowIdx >= 0 ? flowIdx.ToString() : flow.GetHashCode().ToString());
                    entry.GroupName = (CleanLabel(flow.Label) ?? "Workspace") + " / Uncontained";
                    entry.GroupIcon = DefaultIcon(null);
                }
                else
                {
                    entry.GroupKey = containerIds[container];
                    entry.GroupName = containerNames.TryGetValue(container, out var name) ? name : ("Container [" + container.Position.x + ", " + container.Position.y + "]");
                    var sticker = flow.Decorations.Where(d => d.HasSticker && Overlap(container, d.Position, Vector2Int.RoundToInt(d.Size))).OrderBy(d => (d.Position - container.Position).sqrMagnitude).FirstOrDefault();
                    entry.GroupIcon = sticker?.StickerSprite ?? entry.BuildingIcon ?? DefaultIcon(container);
                    entry.GroupTarget = new GlobalLedgerTarget(flow, container: container);
                }

                result.Add(entry);
            }

            // Retain keys for existing frames, but don't retain removed native graphs.
            foreach (var stale in containerIds.Keys.Where(d => !allFlows.Any(f => f.Decorations.Contains(d))).ToArray())
                containerIds.Remove(stale);
            return result;
        }
    }
}
