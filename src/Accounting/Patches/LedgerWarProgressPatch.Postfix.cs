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
    static partial class LedgerWarProgressPatch
    {
        static void Postfix(WarProgress __instance)
        {
            try
            {
                if (warMedals > 0 && GlobalLedgerPlugin.Instance?.Book != null && GamePlayer.Current != null)
                {
                    var war = __instance.War;
                    var allWarEfforts = LedgerLocations.GetAllFlows(GamePlayer.Current)?.SelectMany(f => f.Elements.OfType<WarEffort>()).ToList();
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
                                var source = LedgerObserver.Describe(targets[i]);
                                GlobalLedgerPlugin.Instance.Book.Record(source, Currency.Medal, share, 0);
                            }
                        }
                    }
                    else
                    {
                        string warName = Translation.Translate(war.DisplayName);
                        if (string.IsNullOrEmpty(warName) || warName.StartsWith("@")) warName = war.DisplayName ?? war.Identifier;
                        var source = new LedgerBookSource("war_" + war.Identifier, "War", warName, warName);
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

    }
}
