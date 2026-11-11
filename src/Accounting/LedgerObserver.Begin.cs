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
        internal static LedgerFrame Begin(FlowElement element)
        {
            GlobalLedgerPlugin.Instance?.EnsurePlayer();
            if (GamePlayer.Current == null || current?.Element == element) return null;
            var frame = pool.Count > 0 ? pool.Pop() : new LedgerFrame();
            frame.Element = element; frame.Parent = current; frame.Player = GamePlayer.Current;
            Array.Clear(frame.Income, 0, 9); Array.Clear(frame.Spending, 0, 9); Array.Clear(frame.Tray, 0, 9);
            frame.OutputsBefore.Clear(); frame.VisitorsBefore.Clear(); frame.Produced.Clear(); frame.Consumed.Clear();
            ReadTray(element, frame.Tray);
            SnapshotItemsBefore(element, frame);
            current = frame;
            return frame;
        }

    }
}
