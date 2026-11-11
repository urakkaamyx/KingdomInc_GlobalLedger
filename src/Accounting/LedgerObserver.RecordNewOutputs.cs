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
        static void RecordNewOutputs(FlowElement element, LedgerFrame frame, int startOutput)
        {
            if (element.Outputs == null) return;
            for (int i = startOutput; i < element.Outputs.Count; i++)
            {
                var buffered = element.Outputs[i].BufferedItems;
                for (int j = 0; j < buffered.Count; j++)
                {
                    var item = buffered[j];
                    if (item?.Type != null && !frame.OutputsBefore.Contains(item) && item.Type.Identifier != "dummy_passthrough")
                        AddCount(frame.Produced, item.Type.Identifier, 1);
                }
            }
        }

    }
}
