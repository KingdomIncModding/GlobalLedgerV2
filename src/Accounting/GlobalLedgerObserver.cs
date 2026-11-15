using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KingdomGlobalLedgerV2
{
    internal static class GlobalLedgerObserver
    {
        sealed class ErrorThrottleState
        {
            internal double LastLoggedTime;
            internal int SuppressedCount;
        }

        const double ErrorLogCooldownSeconds = 10.0;
        const int MaxErrorKeys = 64;
        static readonly Dictionary<string, ErrorThrottleState> errorThrottles = new Dictionary<string, ErrorThrottleState>(StringComparer.Ordinal);
        internal static int WarningLogsEmitted { get; private set; }
        internal static int WarningLogsSuppressed { get; private set; }

        internal static void AddCount(Dictionary<string, decimal> map, string key, decimal delta)
        {
            if (string.IsNullOrEmpty(key) || key == "dummy_passthrough")
                return;
            map.TryGetValue(key, out var existing);
            map[key] = GlobalLedgerMath.AddSaturated(existing, delta);
        }

        internal static long ObservedScopes, SkippedScopes, RecordedBatches;
        internal static GlobalLedgerFrame Begin(WorkspaceObject element)
        {
            GlobalLedgerV2Plugin.Instance?.EnsurePlayer();
            if (element == null || FoundationApi.Kingdom == null || current?.Element == element)
                return null;
            var plan = GlobalLedgerObservationPlan.For(element);
            if (plan.Passive || (element.Production.IsRecipe && !element.Production.HasRecipe))
            {
                SkippedScopes++;
                return null;
            }
            ObservedScopes++;
            var frame = pool.Count > 0 ? pool.Pop() : new GlobalLedgerFrame();
            frame.Element = element;
            frame.Plan = plan;
            frame.CaptureOutputs = plan.CaptureOutputs(element);
            frame.CustomNode = element.Behaviour != null;
            frame.Parent = current;
            frame.Player = FoundationApi.Kingdom;
            Array.Clear(frame.Income, 0, 9);
            Array.Clear(frame.Spending, 0, 9);
            Array.Clear(frame.Tray, 0, 9);
            frame.OutputsBefore.Clear();
            frame.VisitorsBefore.Clear();
            frame.ConsumedCandidates.Clear();
            frame.Produced.Clear();
            frame.Consumed.Clear();
            if (plan.Till)
                ReadTray(element, frame.Tray);
            SnapshotItemsBefore(element, frame);
            current = frame;
            return frame;
        }

        internal static GlobalLedgerFrame current;
        static readonly Stack<GlobalLedgerFrame> pool = new Stack<GlobalLedgerFrame>();
        internal static GlobalLedgerBookSource Describe(WorkspaceObject element)
        {
            return GlobalLedgerSourceCache.Describe(element);
        }

        // Balance in till + wallet income makes collecting a transfer, never a second sale.
        internal static void End(GlobalLedgerFrame frame)
        {
            if (frame == null || current != frame)
                return;
            current = frame.Parent;
            try
            {
                if (frame.Player != FoundationApi.Kingdom || GlobalLedgerV2Plugin.Instance == null)
                    return;
                if (frame.Plan.Till)
                {
                    frame.Income[0] = GlobalLedgerMath.AddSaturated(frame.Income[0], frame.Element.Production.CollectedCurrency(GameCurrency.Gold) - frame.Tray[0]);
                    frame.Income[1] = GlobalLedgerMath.AddSaturated(frame.Income[1], frame.Element.Production.CollectedCurrency(GameCurrency.Requisition) - frame.Tray[1]);
                    frame.Income[4] = GlobalLedgerMath.AddSaturated(frame.Income[4], frame.Element.Production.CollectedCurrency(GameCurrency.Mana) - frame.Tray[4]);
                    frame.Income[5] = GlobalLedgerMath.AddSaturated(frame.Income[5], frame.Element.Production.CollectedCurrency(GameCurrency.Crown) - frame.Tray[5]);
                }
                SnapshotItemsAfter(frame.Element, frame);
                if (GlobalLedgerV2Plugin.Instance.Book.RecordBatch(frame))
                    RecordedBatches++;
            }
            finally
            {
                frame.Element = null;
                frame.Parent = null;
                frame.Player = null;
                frame.Plan = null;
                frame.CustomNode = false;
                frame.OutputsBefore.Clear();
                frame.VisitorsBefore.Clear();
                frame.ConsumedCandidates.Clear();
                frame.RemainingVisitors.Clear();
                frame.Produced.Clear();
                frame.Consumed.Clear();
                pool.Push(frame);
            }
        }

        static void Enter(WorkspaceObject __instance, out GlobalLedgerFrame __state) => __state = Begin(__instance);
        internal static void Install(List<IDisposable> hooks)
        {
            hooks.Add(FoundationApi.ExecutionHooks.Subscribe(GlobalLedgerV2Plugin.Id, GameExecutionPoints.BuildingActivity, call => Begin((WorkspaceObject)call.Subject), (call, state) => Leave(state as GlobalLedgerFrame, call.Exception)));
            hooks.Add(FoundationApi.GameHooks.Subscribe(GlobalLedgerV2Plugin.Id, GameHookPoints.CurrencyAdded, after: call => GlobalLedgerIncomePatch.Postfix((GameKingdom)call.Subject, call.Argument<GameCurrency>(0), call.Argument<long>(1), call.Argument<bool>(2))));
            hooks.Add(FoundationApi.GameHooks.Subscribe(GlobalLedgerV2Plugin.Id, GameHookPoints.CurrencySpent, after: call => GlobalLedgerSpendingPatch.Postfix((GameKingdom)call.Subject, call.Argument<GameCurrency>(0), call.Argument<long>(1), call.Result<bool>())));
            hooks.Add(FoundationApi.GameHooks.Subscribe(GlobalLedgerV2Plugin.Id, GameHookPoints.PortConsumed, after: call => GlobalLedgerConsumedPatch.Postfix((WorkspacePort)call.Subject, call.Result<GameInventoryItem>())));
            hooks.Add(FoundationApi.ExecutionHooks.Subscribe(GlobalLedgerV2Plugin.Id, GameExecutionPoints.WarUpdate, call =>
            {
                GlobalLedgerWarProgressPatch.Prefix((GameWarProgress)call.Subject);
                return null;
            }, (call, state) =>
            {
                try
                {
                    if (call.Exception == null)
                        GlobalLedgerWarProgressPatch.Postfix((GameWarProgress)call.Subject);
                }
                finally
                {
                    GlobalLedgerWarProgressPatch.Finalizer(call.Exception);
                }
            }));
            GlobalLedgerCustomNodeHooks.Install(hooks);
        }

        internal static Exception Leave(GlobalLedgerFrame __state, Exception __exception)
        {
            try
            {
                End(__state);
            }
            catch (Exception error)
            {
                ReportObserverError(__state?.Element, error);
            }

            return __exception;
        }

        static void ReportObserverError(WorkspaceObject element, Exception error)
        {
            string typeId = element?.TypeId ?? "unknown";
            string guid = element?.Guid ?? "none";
            string key = error.GetType().FullName + "|" + typeId;
            double now = Time.realtimeSinceStartupAsDouble;
            if (!errorThrottles.TryGetValue(key, out var state))
            {
                if (errorThrottles.Count >= MaxErrorKeys)
                    errorThrottles.Clear();
                errorThrottles[key] = state = new ErrorThrottleState { LastLoggedTime = double.NegativeInfinity };
            }
            if (now - state.LastLoggedTime < ErrorLogCooldownSeconds)
            {
                state.SuppressedCount++;
                WarningLogsSuppressed++;
                return;
            }
            int suppressed = state.SuppressedCount;
            state.SuppressedCount = 0;
            state.LastLoggedTime = now;
            WarningLogsEmitted++;
            string suffix = suppressed > 0 ? " (suppressed " + suppressed + " repeated errors)" : "";
            GlobalLedgerV2Plugin.LogWarningMessage("Accounting error for " + typeId + " [" + guid + "] at batch " + RecordedBatches + suffix + ": " + error);
        }

        static void ReadTray(WorkspaceObject element, decimal[] tray)
        {
            for (int c = 0; c < tray.Length; c++)
                tray[c] = element.Production.CollectedCurrency((GameCurrency)c);
        }

        static void RecordNewOutputs(WorkspaceObject element, GlobalLedgerFrame frame, int startOutput, HashSet<GameInventoryItem> passthrough)
        {
            if (element.Outputs == null)
                return;
            for (int i = startOutput; i < element.Outputs.Count; i++)
            {
                var buffered = element.Outputs[i].BufferedItems;
                for (int j = 0; j < buffered.Count; j++)
                {
                    var item = buffered[j];
                    if (item?.Type != null && !frame.OutputsBefore.Contains(item)
                        && (passthrough == null || !passthrough.Contains(item))
                        && item.Type.Identifier != "dummy_passthrough")
                        AddCount(frame.Produced, item.Type.Identifier, 1);
                }
            }
        }

        internal static void Reset()
        {
            current = null;
            pool.Clear();
            errorThrottles.Clear();
            WarningLogsEmitted = 0;
            WarningLogsSuppressed = 0;
            GlobalLedgerObservationPlan.Reset();
            GlobalLedgerSourceCache.Reset();
            ObservedScopes = SkippedScopes = RecordedBatches = 0;
        }

        static void SnapshotItemsAfter(WorkspaceObject element, GlobalLedgerFrame frame)
        {
            if (frame.VisitorsBefore.Count > 0)
            {
                var remaining = frame.RemainingVisitors;
                remaining.Clear();
                foreach (var visitor in element.Production.Visitors)
                    remaining.Add(visitor);
                int visitorOutputs = element.TypeId == "Arena" ? Math.Min(2, element.Outputs.Count) : Math.Min(1, element.Outputs.Count);
                for (int i = 0; i < visitorOutputs; i++)
                {
                    var buffered = element.Outputs[i].BufferedItems;
                    for (int j = 0; j < buffered.Count; j++)
                        if (buffered[j] != null && !frame.OutputsBefore.Contains(buffered[j]))
                            remaining.Add(buffered[j]);
                }

                foreach (var(visitor, beforeType)in frame.VisitorsBefore)
                {
                    if (remaining.Contains(visitor))
                    {
                        if (visitor.Type != null && beforeType != null && visitor.Type != beforeType)
                        {
                            AddCount(frame.Consumed, beforeType.Identifier, 1);
                            AddCount(frame.Produced, visitor.Type.Identifier, 1);
                        }
                    }
                    else if (beforeType != null)
                        AddCount(frame.Consumed, beforeType.Identifier, 1);
                }
            }

            HashSet<GameInventoryItem> passthrough = null;
            if (frame.ConsumedCandidates.Count > 0)
            {
                var outputsNow = frame.RemainingVisitors;
                outputsNow.Clear();
                if (element.Outputs != null)
                {
                    for (int i = 0; i < element.Outputs.Count; i++)
                    {
                        var buffered = element.Outputs[i].BufferedItems;
                        for (int j = 0; j < buffered.Count; j++)
                            if (buffered[j] != null) outputsNow.Add(buffered[j]);
                    }
                }
                for (int i = 0; i < frame.ConsumedCandidates.Count; i++)
                {
                    var consumedItem = frame.ConsumedCandidates[i];
                    if (outputsNow.Contains(consumedItem) && !frame.OutputsBefore.Contains(consumedItem))
                    {
                        if (passthrough == null) passthrough = new HashSet<GameInventoryItem>();
                        passthrough.Add(consumedItem);
                    }
                    else if (consumedItem?.Type != null)
                    {
                        AddCount(frame.Consumed, consumedItem.Type.Identifier, 1);
                    }
                }
            }

            if (frame.CaptureOutputs)
                RecordNewOutputs(element, frame, element.TypeId == "QuestBoard" ? 1 : 0, passthrough);
        }

        static void SnapshotItemsBefore(WorkspaceObject element, GlobalLedgerFrame frame)
        {
            if (frame.CaptureOutputs || frame.Plan.Visitors)
                foreach (var output in element.Outputs)
                    foreach (var item in output.BufferedItems)
                        if (item != null)
                            frame.OutputsBefore.Add(item);
            if (frame.Plan.Visitors)
                foreach (var item in element.Production.Visitors)
                    if (element.TypeId != "Arena" || item.Type?.IsHuman == true)
                        frame.VisitorsBefore.Add((item, item.Type));
        }
    }
}
