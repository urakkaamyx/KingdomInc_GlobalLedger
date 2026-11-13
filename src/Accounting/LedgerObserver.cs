using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace KingdomGlobalLedger
{
    internal static class LedgerObserver
    {
        internal static void AddCount(Dictionary<string, long> map, string key, long delta)
        {
            if (string.IsNullOrEmpty(key) || key == "dummy_passthrough")
                return;
            map.TryGetValue(key, out var existing);
            map[key] = existing + delta;
        }

        internal static LedgerFrame Begin(WorkspaceObject element)
        {
            GlobalLedgerPlugin.Instance?.EnsurePlayer();
            if (FoundationApi.Kingdom == null || current?.Element == element)
                return null;
            var frame = pool.Count > 0 ? pool.Pop() : new LedgerFrame();
            frame.Element = element;
            frame.Parent = current;
            frame.Player = FoundationApi.Kingdom;
            Array.Clear(frame.Income, 0, 9);
            Array.Clear(frame.Spending, 0, 9);
            Array.Clear(frame.Tray, 0, 9);
            frame.OutputsBefore.Clear();
            frame.VisitorsBefore.Clear();
            frame.Produced.Clear();
            frame.Consumed.Clear();
            ReadTray(element, frame.Tray);
            SnapshotItemsBefore(element, frame);
            current = frame;
            return frame;
        }

        internal static LedgerFrame current;
        static readonly Stack<LedgerFrame> pool = new Stack<LedgerFrame>();
        internal static LedgerBookSource Describe(WorkspaceObject element)
        {
            string label = element.Production.DefaultLabel;
            string typeName = FoundationApi.GameInfo.Translate(label);
            if (string.IsNullOrEmpty(typeName) || typeName.StartsWith("@"))
                typeName = Regex.Replace(element.TypeId, "([a-z])([A-Z])", "$1 $2");
            var flow = element.ParentWorkspace;
            string detail = element.Production.HasRecipe ? FoundationApi.GameInfo.Translate(element.Production.RecipeDisplayName) : FoundationApi.GameInfo.Translate(element.Label);
            string name = (LedgerLocations.CleanLabel(flow?.Label) ?? "Workspace") + " / " + detail + " [" + element.Position.x + ", " + element.Position.y + "]";
            return new LedgerBookSource(element.Guid, element.TypeId, typeName, name);
        }

        // Balance in till + wallet income makes collecting a transfer, never a second sale.
        internal static void End(LedgerFrame frame)
        {
            if (frame == null || current != frame)
                return;
            current = frame.Parent;
            try
            {
                if (frame.Player != FoundationApi.Kingdom || GlobalLedgerPlugin.Instance == null)
                    return;
                long gold = frame.Element.Production.CollectedCurrency(GameCurrency.Gold), mana = frame.Element.Production.CollectedCurrency(GameCurrency.Mana), crowns = frame.Element.Production.CollectedCurrency(GameCurrency.Crown), req = frame.Element.Production.CollectedCurrency(GameCurrency.Requisition);
                frame.Income[0] += gold - frame.Tray[0];
                frame.Income[1] += req - frame.Tray[1];
                frame.Income[4] += mana - frame.Tray[4];
                frame.Income[5] += crowns - frame.Tray[5];
                SnapshotItemsAfter(frame.Element, frame);
                LedgerBookSource source = null;
                for (int c = 0; c < 9; c++)
                {
                    long earned = Math.Max(0, frame.Income[c]);
                    if (earned == 0 && frame.Spending[c] == 0)
                        continue;
                    if (source == null)
                        source = Describe(frame.Element);
                    GlobalLedgerPlugin.Instance.Book.Record(source, (GameCurrency)c, earned, frame.Spending[c]);
                }

                foreach (var itemId in frame.Produced.Keys.Union(frame.Consumed.Keys))
                {
                    frame.Produced.TryGetValue(itemId, out var produced);
                    frame.Consumed.TryGetValue(itemId, out var consumed);
                    if (produced == 0 && consumed == 0)
                        continue;
                    if (source == null)
                        source = Describe(frame.Element);
                    GlobalLedgerPlugin.Instance.Book.RecordItem(source, itemId, produced, consumed);
                }
            }
            finally
            {
                frame.Element = null;
                frame.Parent = null;
                frame.Player = null;
                frame.OutputsBefore.Clear();
                frame.VisitorsBefore.Clear();
                pool.Push(frame);
            }
        }

        static void Enter(WorkspaceObject __instance, out LedgerFrame __state) => __state = Begin(__instance);
        internal static void Install(List<IDisposable> hooks)
        {
            hooks.Add(FoundationApi.ExecutionHooks.Subscribe(GlobalLedgerPlugin.Id, GameExecutionPoints.BuildingActivity, call => Begin((WorkspaceObject)call.Subject), (call, state) => Leave(state as LedgerFrame, call.Exception)));
            hooks.Add(FoundationApi.GameHooks.Subscribe(GlobalLedgerPlugin.Id, GameHookPoints.CurrencyAdded, after: call => LedgerIncomePatch.Postfix((GameKingdom)call.Subject, call.Argument<GameCurrency>(0), call.Argument<long>(1), call.Argument<bool>(2))));
            hooks.Add(FoundationApi.GameHooks.Subscribe(GlobalLedgerPlugin.Id, GameHookPoints.CurrencySpent, after: call => LedgerSpendingPatch.Postfix((GameKingdom)call.Subject, call.Argument<GameCurrency>(0), call.Argument<long>(1), call.Result<bool>())));
            hooks.Add(FoundationApi.GameHooks.Subscribe(GlobalLedgerPlugin.Id, GameHookPoints.PortConsumed, after: call => LedgerConsumedPatch.Postfix((WorkspacePort)call.Subject, call.Result<GameInventoryItem>())));
            hooks.Add(FoundationApi.ExecutionHooks.Subscribe(GlobalLedgerPlugin.Id, GameExecutionPoints.WarUpdate, call =>
            {
                LedgerWarProgressPatch.Prefix((GameWarProgress)call.Subject);
                return null;
            }, (call, state) =>
            {
                try
                {
                    if (call.Exception == null)
                        LedgerWarProgressPatch.Postfix((GameWarProgress)call.Subject);
                }
                finally
                {
                    LedgerWarProgressPatch.Finalizer(call.Exception);
                }
            }));
        }

        static Exception Leave(LedgerFrame __state, Exception __exception)
        {
            try
            {
                End(__state);
            }
            catch (Exception error)
            {
                BepInEx.Logging.Logger.CreateLogSource("Global Ledger").LogWarning(error);
            }

            return __exception;
        }

        static void ReadTray(WorkspaceObject element, long[] tray)
        {
            for (int c = 0; c < tray.Length; c++)
                tray[c] = element.Production.CollectedCurrency((GameCurrency)c);
        }

        static void RecordNewOutputs(WorkspaceObject element, LedgerFrame frame, int startOutput)
        {
            if (element.Outputs == null)
                return;
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

        internal static void Reset()
        {
            current = null;
            pool.Clear();
        }

        static void SnapshotItemsAfter(WorkspaceObject element, LedgerFrame frame)
        {
            if (frame.VisitorsBefore.Count > 0)
            {
                var remaining = new HashSet<GameInventoryItem>();
                foreach (var visitor in element.Production.Visitors)
                    remaining.Add(visitor);
                int visitorOutputs = element.TypeId == "Arena" ? Math.Min(2, element.Outputs.Count) : Math.Min(1, element.Outputs.Count);
                for (int i = 0; i < visitorOutputs; i++)
                {
                    var buffered = element.Outputs[i].BufferedItems;
                    for (int j = 0; j < buffered.Count; j++)
                        if (buffered[j] != null && !frame.OutputsBefore.Contains(buffered[j]))
                            remaining.Add(buffered[j]);
                }

                foreach (var(visitor, beforeType)in frame.VisitorsBefore)
                {
                    if (remaining.Contains(visitor))
                    {
                        if (visitor.Type != null && beforeType != null && visitor.Type != beforeType)
                        {
                            AddCount(frame.Consumed, beforeType.Identifier, 1);
                            AddCount(frame.Produced, visitor.Type.Identifier, 1);
                        }
                    }
                    else if (beforeType != null)
                        AddCount(frame.Consumed, beforeType.Identifier, 1);
                }
            }

            if (element.Production.IsResource || new[]
            {
                "Forest",
                "Dungeon",
                "WarEffort",
                "DevSpawner"
            }.Contains(element.TypeId))
                RecordNewOutputs(element, frame, 0);
            else if (element.Production.IsRecipe && !element.Production.HasEnhancementResult)
                RecordNewOutputs(element, frame, 0);
            else if (element.TypeId == "QuestBoard")
                RecordNewOutputs(element, frame, 1);
        }

        static void SnapshotItemsBefore(WorkspaceObject element, LedgerFrame frame)
        {
            foreach (var output in element.Outputs)
                foreach (var item in output.BufferedItems)
                    if (item != null)
                        frame.OutputsBefore.Add(item);
            foreach (var item in element.Production.Visitors)
                if (element.TypeId != "Arena" || item.Type?.IsHuman == true)
                    frame.VisitorsBefore.Add((item, item.Type));
        }
    }
}
