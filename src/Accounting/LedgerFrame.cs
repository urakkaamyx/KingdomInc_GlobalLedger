using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Assets.Source.Adventure;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Items.Type;
using Assets.Source.Player;
using Assets.Source.Util;
using HarmonyLib;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerFrame
    {
        internal FlowElement Element;
        internal LedgerFrame Parent;
        internal GamePlayer Player;
        internal readonly long[] Income = new long[9], Spending = new long[9], Tray = new long[9];
        internal readonly HashSet<InventoryItem> OutputsBefore = new HashSet<InventoryItem>();
        internal readonly List<(InventoryItem item, ItemType type)> VisitorsBefore = new List<(InventoryItem, ItemType)>();
        internal readonly Dictionary<string, long> Produced = new Dictionary<string, long>(), Consumed = new Dictionary<string, long>();

    }
}
