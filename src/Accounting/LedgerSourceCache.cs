using KingdomFoundation.Api;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;

namespace KingdomGlobalLedger
{
    internal sealed class LedgerSourceCache
    {
        static ConditionalWeakTable<WorkspaceObject, LedgerSourceCache> cache = new ConditionalWeakTable<WorkspaceObject, LedgerSourceCache>();
        static readonly Regex Words = new Regex("([a-z])([A-Z])", RegexOptions.Compiled);
        LedgerBookSource source;
        string label, recipe, flowLabel;
        Vector2Int position;
        GameWorkspace workspace;

        internal static LedgerBookSource Describe(WorkspaceObject element)
            => cache.GetValue(element, value => new LedgerSourceCache()).Read(element);

        LedgerBookSource Read(WorkspaceObject element)
        {
            var flow = GlobalLedgerPlugin.Instance?.Locations.WorkspaceOf(element) ?? element.ParentWorkspace;
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
            string name = (LedgerLocations.CleanLabel(flowLabel) ?? "Workspace") + " / " + detail + " [" + position.x + ", " + position.y + "]";
            return source = new LedgerBookSource(element.Guid, element.TypeId, typeName, name);
        }

        internal static void Reset() => cache = new ConditionalWeakTable<WorkspaceObject, LedgerSourceCache>();
    }
}
