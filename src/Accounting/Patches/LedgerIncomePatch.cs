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
    [HarmonyPatch(typeof(GamePlayer), nameof(GamePlayer.AddCurrency), new[] { typeof(Currency), typeof(long), typeof(bool) })]
    static partial class LedgerIncomePatch
    {

    }
}
