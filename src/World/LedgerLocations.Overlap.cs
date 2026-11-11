using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Assets.Source.Adventure;
using Assets.Source.Flow;
using Assets.Source.Player;
using Assets.Source.Util;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerLocations
    {
        internal static bool Overlap(FlowDeco container, Vector2Int position, Vector2Int size)
        {
            return container.Position.x < (long)position.x + size.x && (long)container.Position.x + container.Size.x > position.x
                && container.Position.y > (long)position.y - size.y && (long)container.Position.y - container.Size.y < position.y;
        }

    }
}
