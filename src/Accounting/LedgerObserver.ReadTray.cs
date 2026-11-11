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
    internal static partial class LedgerObserver
    {
        static void ReadTray(FlowElement element, long[] tray)
        {
            switch (element)
            {
                case Shop shop: tray[0] = shop.CollectedGold; break;
                case Disenchanter disenchanter: tray[4] = disenchanter.CollectedMana; break;
                case Restaurant restaurant: tray[0] = restaurant.CollectedGold; tray[5] = restaurant.CollectedCrowns; break;
                case RequisitionOffice office: tray[1] = office.CollectedRequisition; break;
            }
        }

    }
}
