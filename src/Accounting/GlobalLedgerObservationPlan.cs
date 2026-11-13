using KingdomFoundation.Api;
using System.Runtime.CompilerServices;

namespace KingdomGlobalLedgerV2
{
    /// <summary>Feature-side choices about which existing Foundation values need observation.</summary>
    internal sealed class GlobalLedgerObservationPlan
    {
        static ConditionalWeakTable<WorkspaceObject, GlobalLedgerObservationPlan> cache = new ConditionalWeakTable<WorkspaceObject, GlobalLedgerObservationPlan>();
        internal readonly bool Passive;
        internal readonly bool Outputs;
        internal readonly bool Visitors;
        internal readonly bool Till;

        GlobalLedgerObservationPlan(WorkspaceObject element)
        {
            string type = element.TypeId;
            // Only source-confirmed native transport/storage types are excluded. Unknown types
            // retain the general observation scope so mod-owned currency activity is attributed.
            switch (type)
            {
                case "Collector": case "Distributor": case "Manifold": case "Stockpile":
                case "Filter": case "TrackSwitch": case "Timer": case "SingleButton":
                    Passive = true;
                    break;
            }
            var production = element.Production;
            Outputs = production.IsResource || (production.IsRecipe && !production.HasEnhancementResult)
                || type == "Forest" || type == "Dungeon" || type == "WarEffort" || type == "DevSpawner" || type == "QuestBoard";
            Visitors = type == "TrainingGrounds" || type == "Arena" || type == "QuestBoard";
            // Unknown/custom types retain till observation. Only types known not to hold
            // uncollected currency can safely skip these reads through today's API.
            Till = !(Passive || production.IsResource || production.IsRecipe || Visitors
                || type == "Forest" || type == "Dungeon" || type == "WarEffort" || type == "DevSpawner");
        }

        // Recipe and enhancement choices may change without replacing the building identity.
        // Re-evaluate those flags per scope; the remaining flags depend only on building type.
        internal bool CaptureOutputs(WorkspaceObject element) => element.Production.IsRecipe
            ? !element.Production.HasEnhancementResult : Outputs;

        internal static GlobalLedgerObservationPlan For(WorkspaceObject element) => cache.GetValue(element, value => new GlobalLedgerObservationPlan(value));
        internal static void Reset() => cache = new ConditionalWeakTable<WorkspaceObject, GlobalLedgerObservationPlan>();
    }
}
