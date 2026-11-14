using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    internal sealed class LedgerTarget
    {
        internal LedgerTarget(GameWorkspace flow, WorkspaceObject building = null, WorkspaceObject container = null)
        {
            Flow = flow;
            Building = building;
            Container = container;
        }

        internal readonly GameWorkspace Flow;
        internal readonly WorkspaceObject Building;
        internal readonly WorkspaceObject Container;
        internal bool Exists => FoundationApi.Kingdom != null && GlobalLedgerPlugin.Instance.Locations.AllFlows().Contains(Flow) && (Building != null ? Flow.Buildings.Contains(Building) : Container != null && Flow.Decorations.Contains(Container));
        internal RectTransform View => Building != null ? (Building.World ? (RectTransform)Building.World.transform : null) : Container?.World ? (RectTransform)Container.World.transform : null;

        internal static bool Same(LedgerTarget a, LedgerTarget b) => ReferenceEquals(a, b) || a != null && b != null && ReferenceEquals(a.Flow, b.Flow) && ReferenceEquals(a.Building, b.Building) && ReferenceEquals(a.Container, b.Container);
    }
}
