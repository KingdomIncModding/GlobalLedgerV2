using KingdomFoundation.Api;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;

namespace KingdomGlobalLedgerV2
{
    internal sealed class GlobalLedgerSourceCache
    {
        static ConditionalWeakTable<WorkspaceObject, GlobalLedgerSourceCache> cache = new ConditionalWeakTable<WorkspaceObject, GlobalLedgerSourceCache>();
        static readonly Regex Words = new Regex("([a-z])([A-Z])", RegexOptions.Compiled);
        GlobalLedgerBookSource source;
        string label, recipe, flowLabel;
        Vector2Int position;
        GameWorkspace workspace;

        internal static GlobalLedgerBookSource Describe(WorkspaceObject element)
            => cache.GetValue(element, value => new GlobalLedgerSourceCache()).Read(element);

        GlobalLedgerBookSource Read(WorkspaceObject element)
        {
            var flow = GlobalLedgerV2Plugin.Instance?.Locations.WorkspaceOf(element) ?? element.ParentWorkspace;
            string nextLabel = element.Label;
            string nextRecipe = element.Production.RecipeDisplayName;
            string nextFlowLabel = flow?.Label;
            if (source != null && workspace == flow && label == nextLabel && recipe == nextRecipe
                && flowLabel == nextFlowLabel && position == element.Position)
                return source;
            workspace = flow;
            label = nextLabel;
            recipe = nextRecipe;
            flowLabel = nextFlowLabel;
            position = element.Position;
            string typeName = FoundationApi.GameInfo.Translate(element.Production.DefaultLabel);
            if (string.IsNullOrEmpty(typeName) || typeName.StartsWith("@"))
                typeName = Words.Replace(element.TypeId, "$1 $2");
            string detail = FoundationApi.GameInfo.Translate(element.Production.HasRecipe ? recipe : label);
            string name = (GlobalLedgerLocations.CleanLabel(flowLabel) ?? "Workspace") + " / " + detail + " [" + position.x + ", " + position.y + "]";
            return source = new GlobalLedgerBookSource(element.Guid, element.TypeId, typeName, name);
        }

        internal static void Reset() => cache = new ConditionalWeakTable<WorkspaceObject, GlobalLedgerSourceCache>();
    }
}
