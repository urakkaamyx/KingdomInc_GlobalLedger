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
        static void SnapshotItemsBefore(FlowElement element, LedgerFrame frame)
        {
            if (element.Outputs != null)
                for (int i = 0; i < element.Outputs.Count; i++)
                {
                    var buffered = element.Outputs[i].BufferedItems;
                    for (int j = 0; j < buffered.Count; j++)
                        if (buffered[j] != null) frame.OutputsBefore.Add(buffered[j]);
                }
            switch (element)
            {
                case TrainingGrounds training:
                    for (int i = 0; i < training.Contents.Length; i++)
                        if (training.Contents[i]?.Human != null) frame.VisitorsBefore.Add((training.Contents[i].Human, training.Contents[i].Human.Type));
                    break;
                case Arena arena:
                    if (arena.Slot1?.Item != null && arena.Slot1.Item.Type is HumanItemType) frame.VisitorsBefore.Add((arena.Slot1.Item, arena.Slot1.Item.Type));
                    if (arena.Slot2?.Item != null && arena.Slot2.Item.Type is HumanItemType) frame.VisitorsBefore.Add((arena.Slot2.Item, arena.Slot2.Item.Type));
                    break;
                case QuestBoard quest:
                    for (int i = 0; i < quest.Contents.Count; i++)
                        if (quest.Contents[i]?.Item != null) frame.VisitorsBefore.Add((quest.Contents[i].Item, quest.Contents[i].Item.Type));
                    break;
            }
        }

    }
}
