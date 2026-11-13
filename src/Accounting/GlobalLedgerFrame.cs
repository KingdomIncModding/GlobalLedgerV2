using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace KingdomGlobalLedgerV2
{
    internal sealed class GlobalLedgerFrame
    {
        internal WorkspaceObject Element;
        internal GlobalLedgerFrame Parent;
        internal GameKingdom Player;
        internal GlobalLedgerObservationPlan Plan;
        internal bool CaptureOutputs;
        internal readonly long[] Income = new long[9], Spending = new long[9], Tray = new long[9];
        internal readonly HashSet<GameInventoryItem> OutputsBefore = new HashSet<GameInventoryItem>();
        internal readonly List<(GameInventoryItem item, GameItemType type)> VisitorsBefore = new List<(GameInventoryItem, GameItemType)>();
        internal readonly HashSet<GameInventoryItem> RemainingVisitors = new HashSet<GameInventoryItem>();
        internal readonly Dictionary<string, long> Produced = new Dictionary<string, long>(), Consumed = new Dictionary<string, long>();
    }
}
