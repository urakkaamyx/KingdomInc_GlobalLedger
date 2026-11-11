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
        static void SnapshotItemsAfter(FlowElement element, LedgerFrame frame)
        {
            if (frame.VisitorsBefore.Count > 0)
            {
                var remaining = new HashSet<InventoryItem>();
                switch (element)
                {
                    case TrainingGrounds training:
                        for (int i = 0; i < training.Contents.Length; i++)
                            if (training.Contents[i]?.Human != null) remaining.Add(training.Contents[i].Human);
                        break;
                    case Arena arena:
                        if (arena.Slot1?.Item != null) remaining.Add(arena.Slot1.Item);
                        if (arena.Slot2?.Item != null) remaining.Add(arena.Slot2.Item);
                        break;
                    case QuestBoard quest:
                        for (int i = 0; i < quest.Contents.Count; i++)
                            if (quest.Contents[i]?.Item != null) remaining.Add(quest.Contents[i].Item);
                        break;
                }
                int visitorOutputs = element is Arena ? Math.Min(2, element.Outputs.Count) : Math.Min(1, element.Outputs.Count);
                for (int i = 0; i < visitorOutputs; i++)
                {
                    var buffered = element.Outputs[i].BufferedItems;
                    for (int j = 0; j < buffered.Count; j++)
                        if (buffered[j] != null && !frame.OutputsBefore.Contains(buffered[j])) remaining.Add(buffered[j]);
                }
                foreach (var (visitor, beforeType) in frame.VisitorsBefore)
                {
                    if (remaining.Contains(visitor))
                    {
                        if (visitor.Type != null && beforeType != null && visitor.Type != beforeType)
                        {
                            AddCount(frame.Consumed, beforeType.Identifier, 1);
                            AddCount(frame.Produced, visitor.Type.Identifier, 1);
                        }
                    }
                    else if (beforeType != null) AddCount(frame.Consumed, beforeType.Identifier, 1);
                }
            }
            if (element is ResourceElement || element is Forest || element is Dungeon || element is WarEffort || element is DevSpawner)
                RecordNewOutputs(element, frame, 0);
            else if (element is RecipeElement recipe && (recipe.Recipe?.Results == null || !recipe.Recipe.Results.Any(r => r.Enhancement != null)))
                RecordNewOutputs(element, frame, 0);
            else if (element is QuestBoard)
                RecordNewOutputs(element, frame, 1);
        }

    }
}
