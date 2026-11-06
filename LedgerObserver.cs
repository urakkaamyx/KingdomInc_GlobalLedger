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
    internal static class LedgerObserver
    {
        internal sealed class Frame
        {
            internal FlowElement Element;
            internal Frame Parent;
            internal GamePlayer Player;
            internal readonly long[] Income = new long[9], Spending = new long[9], Tray = new long[9];
            internal readonly HashSet<InventoryItem> OutputsBefore = new HashSet<InventoryItem>();
            internal readonly List<(InventoryItem item, ItemType type)> VisitorsBefore = new List<(InventoryItem, ItemType)>();
            internal readonly Dictionary<string, long> Produced = new Dictionary<string, long>(), Consumed = new Dictionary<string, long>();
        }
        static Frame current;
        static readonly Stack<Frame> pool = new Stack<Frame>();
        internal static void Reset() { current = null; pool.Clear(); }
        internal static Frame Begin(FlowElement element)
        {
            GlobalLedgerPlugin.Instance?.EnsurePlayer();
            if (GamePlayer.Current == null || current?.Element == element) return null;
            var frame = pool.Count > 0 ? pool.Pop() : new Frame();
            frame.Element = element; frame.Parent = current; frame.Player = GamePlayer.Current;
            Array.Clear(frame.Income, 0, 9); Array.Clear(frame.Spending, 0, 9); Array.Clear(frame.Tray, 0, 9);
            frame.OutputsBefore.Clear(); frame.VisitorsBefore.Clear(); frame.Produced.Clear(); frame.Consumed.Clear();
            ReadTray(element, frame.Tray);
            SnapshotItemsBefore(element, frame);
            current = frame;
            return frame;
        }
        // Balance in till + wallet income makes collecting a transfer, never a second sale.
        internal static void End(Frame frame)
        {
            if (frame == null || current != frame) return;
            current = frame.Parent;
            try
            {
                if (frame.Player != GamePlayer.Current || GlobalLedgerPlugin.Instance == null) return;
                long gold = 0, mana = 0, crowns = 0, req = 0;
                switch (frame.Element)
                {
                    case Shop shop: gold = shop.CollectedGold; break;
                    case Disenchanter disenchanter: mana = disenchanter.CollectedMana; break;
                    case Restaurant restaurant: gold = restaurant.CollectedGold; crowns = restaurant.CollectedCrowns; break;
                    case RequisitionOffice office: req = office.CollectedRequisition; break;
                }
                frame.Income[0] += gold - frame.Tray[0];
                frame.Income[1] += req - frame.Tray[1];
                frame.Income[4] += mana - frame.Tray[4];
                frame.Income[5] += crowns - frame.Tray[5];
                SnapshotItemsAfter(frame.Element, frame);
                LedgerBook.Source source = null;
                for (int c = 0; c < 9; c++)
                {
                    long earned = Math.Max(0, frame.Income[c]);
                    if (earned == 0 && frame.Spending[c] == 0) continue;
                    if (source == null) source = Describe(frame.Element);
                    GlobalLedgerPlugin.Instance.Book.Record(source, (Currency)c, earned, frame.Spending[c]);
                }
                foreach (var itemId in frame.Produced.Keys.Union(frame.Consumed.Keys))
                {
                    frame.Produced.TryGetValue(itemId, out var produced);
                    frame.Consumed.TryGetValue(itemId, out var consumed);
                    if (produced == 0 && consumed == 0) continue;
                    if (source == null) source = Describe(frame.Element);
                    GlobalLedgerPlugin.Instance.Book.RecordItem(source, itemId, produced, consumed);
                }
            }
            finally { frame.Element = null; frame.Parent = null; frame.Player = null; frame.OutputsBefore.Clear(); frame.VisitorsBefore.Clear(); pool.Push(frame); }
        }
        static void SnapshotItemsBefore(FlowElement element, Frame frame)
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
        static void SnapshotItemsAfter(FlowElement element, Frame frame)
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
        static void RecordNewOutputs(FlowElement element, Frame frame, int startOutput)
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
        static void AddCount(Dictionary<string, long> map, string key, long delta)
        {
            if (string.IsNullOrEmpty(key) || key == "dummy_passthrough") return;
            map.TryGetValue(key, out var existing);
            map[key] = existing + delta;
        }
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
        internal static LedgerBook.Source Describe(FlowElement element)
        {
            string label = element is ResourceElement resource ? resource.DefaultLabel : element is RecipeElement recipe ? recipe.DefaultLabel : element.Label;
            string typeName = Translation.Translate(label);
            if (string.IsNullOrEmpty(typeName) || typeName.StartsWith("@")) typeName = Regex.Replace(element.ElementType, "([a-z])([A-Z])", "$1 $2");
            var flow = element.ParentFlow;
            string detail = element is RecipeElement crafting && crafting.Recipe != null ? Translation.Translate(crafting.Recipe.DisplayName) : Translation.Translate(element.Label);
            string name = (LedgerLocations.CleanLabel(flow?.Label) ?? "Workspace") + " / " + detail + " [" + element.Position.x + ", " + element.Position.y + "]";
            return new LedgerBook.Source(element.Guid, element.ElementType, typeName, name);
        }
        internal static void Install(Harmony harmony)
        {
            var prefix = new HarmonyMethod(typeof(LedgerObserver), nameof(Enter));
            var finalizer = new HarmonyMethod(typeof(LedgerObserver), nameof(Leave));
            var names = new HashSet<string> { "Update", "DoManualCraft", "DoCollect", "QueueCraft", "SoldierDied", "UpdateQuestProgress" };
            foreach (var type in typeof(FlowElement).Assembly.GetTypes().Where(t => typeof(FlowElement).IsAssignableFrom(t)))
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => names.Contains(m.Name) && !m.IsAbstract && !m.ContainsGenericParameters))
                    harmony.Patch(method, prefix: prefix, finalizer: finalizer);
            harmony.CreateClassProcessor(typeof(Income)).Patch();
            harmony.CreateClassProcessor(typeof(Spending)).Patch();
            harmony.CreateClassProcessor(typeof(Consumed)).Patch();
            harmony.CreateClassProcessor(typeof(WarProgressPatch)).Patch();
        }
        static void Enter(FlowElement __instance, out Frame __state) => __state = Begin(__instance);
        static Exception Leave(Frame __state, Exception __exception)
        {
            try { End(__state); }
            catch (Exception error) { BepInEx.Logging.Logger.CreateLogSource("Global Ledger").LogWarning(error); }
            return __exception;
        }
        [HarmonyPatch(typeof(GamePlayer), nameof(GamePlayer.AddCurrency), new[] { typeof(Currency), typeof(long), typeof(bool) })]
        static class Income
        {
            static void Postfix(GamePlayer __instance, Currency c, long amt, bool isIncome)
            {
                if (!isIncome || amt <= 0) return;
                if (current != null && current.Player == __instance) current.Income[(int)c] += amt;
                else if (current == null) WarProgressPatch.RecordIncome(c, amt);
            }
        }
        [HarmonyPatch(typeof(WarProgress), nameof(WarProgress.UpdateWar))]
        static class WarProgressPatch
        {
            static WarProgress activeWar;
            static long warMedals;

            static void Prefix(WarProgress __instance)
            {
                activeWar = __instance;
                warMedals = 0;
            }

            static void Postfix(WarProgress __instance)
            {
                try
                {
                    if (warMedals > 0 && GlobalLedgerPlugin.Instance?.Book != null && GamePlayer.Current != null)
                    {
                        var war = __instance.War;
                        var allWarEfforts = GamePlayer.Current.Flows?.SelectMany(f => f.Elements.OfType<WarEffort>()).ToList();
                        var warEfforts = allWarEfforts?.Where(w => w != null && w.WarType == war).ToList();

                        if (warEfforts != null && warEfforts.Count > 0)
                        {
                            var connected = warEfforts.Where(w => w.Inputs != null && w.Inputs.Count > 0 && w.Inputs[0].ConnectedPipe != null).ToList();
                            var targets = connected.Count > 0 ? connected : warEfforts;
                            long baseShare = warMedals / targets.Count;
                            long remainder = warMedals % targets.Count;
                            for (int i = 0; i < targets.Count; i++)
                            {
                                long share = baseShare + (i < remainder ? 1 : 0);
                                if (share > 0)
                                {
                                    var source = Describe(targets[i]);
                                    GlobalLedgerPlugin.Instance.Book.Record(source, Currency.Medal, share, 0);
                                }
                            }
                        }
                        else
                        {
                            string warName = Translation.Translate(war.DisplayName);
                            if (string.IsNullOrEmpty(warName) || warName.StartsWith("@")) warName = war.DisplayName ?? war.Identifier;
                            var source = new LedgerBook.Source("war_" + war.Identifier, "War", warName, warName);
                            GlobalLedgerPlugin.Instance.Book.Record(source, Currency.Medal, warMedals, 0);
                        }
                    }
                }
                finally
                {
                    activeWar = null;
                    warMedals = 0;
                }
            }

            static Exception Finalizer(Exception __exception)
            {
                activeWar = null;
                warMedals = 0;
                return __exception;
            }

            internal static void RecordIncome(Currency c, long amt)
            {
                if (activeWar != null && c == Currency.Medal && amt > 0)
                    warMedals += amt;
            }
        }
        [HarmonyPatch(typeof(GamePlayer), nameof(GamePlayer.SpendCurrency), new[] { typeof(Currency), typeof(long) })]
        static class Spending
        {
            static void Postfix(GamePlayer __instance, Currency c, long amt, bool __result)
            { if (__result && amt > 0 && current != null && current.Player == __instance) current.Spending[(int)c] += amt; }
        }
        [HarmonyPatch(typeof(FlowIO), nameof(FlowIO.ConsumeItem), new Type[0])]
        static class Consumed
        {
            static void Postfix(FlowIO __instance, InventoryItem __result)
            {
                if (current == null || __result?.Type == null || !(__instance is FlowInput) || __instance.Parent != current.Element) return;
                var element = current.Element;
                int index = __instance.ParentIndex;
                if (element is TrainingGrounds && index == 0 || element is QuestBoard && index == 0 || element is Arena && __result.Type is HumanItemType)
                {
                    if (!current.VisitorsBefore.Any(v => ReferenceEquals(v.item, __result)))
                        current.VisitorsBefore.Add((__result, __result.Type));
                    return;
                }
                if (IsConsumedInput(element, index, __result))
                    AddCount(current.Consumed, __result.Type.Identifier, 1);
            }
            static bool IsConsumedInput(FlowElement element, int inputIndex, InventoryItem item)
            {
                switch (element)
                {
                    case RecipeElement recipe:
                        if (inputIndex == 0 && recipe.Recipe?.Results != null && recipe.Recipe.Results.Any(r => r.Enhancement != null))
                            return false;
                        return true;
                    case Shop _:
                    case LuxuryShop _:
                    case Restaurant _:
                    case Disenchanter _:
                    case RequisitionOffice _:
                    case NightmareCollector _:
                    case Incinerator _:
                    case Dungeon _:
                    case WarEffort _:
                        return true;
                    case Outfitter _:
                    case Inn _:
                    case TrainingGrounds _:
                        return inputIndex == 1;
                    case Arena _:
                        return !(item.Type is HumanItemType);
                    default:
                        return false;
                }
            }
        }
    }
}
