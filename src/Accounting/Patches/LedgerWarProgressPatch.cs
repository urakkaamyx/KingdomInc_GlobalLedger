using KingdomFoundation.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace KingdomGlobalLedger
{
    static class LedgerWarProgressPatch
    {
        static GameWarProgress activeWar;
        static long warMedals;
        internal static Exception Finalizer(Exception __exception)
        {
            activeWar = null;
            warMedals = 0;
            return __exception;
        }

        internal static void Postfix(GameWarProgress __instance)
        {
            try
            {
                if (warMedals > 0 && GlobalLedgerPlugin.Instance?.Book != null && FoundationApi.Kingdom != null)
                {
                    var war = __instance.War;
                    var allWarEfforts = LedgerLocations.GetAllFlows(FoundationApi.Kingdom)?.SelectMany(f => f.Buildings.Where(element => element.TypeId == "WarEffort")).ToList();
                    var warEfforts = allWarEfforts?.Where(w => w != null && w.Production.War == war).ToList();
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
                                GlobalLedgerPlugin.Instance.Book.Record(source, GameCurrency.Medal, share, 0);
                            }
                        }
                    }
                    else
                    {
                        string warName = FoundationApi.GameInfo.Translate(war.DisplayName);
                        if (string.IsNullOrEmpty(warName) || warName.StartsWith("@"))
                            warName = war.DisplayName ?? war.Identifier;
                        var source = new LedgerBookSource("war_" + war.Identifier, "War", warName, warName);
                        GlobalLedgerPlugin.Instance.Book.Record(source, GameCurrency.Medal, warMedals, 0);
                    }
                }
            }
            finally
            {
                activeWar = null;
                warMedals = 0;
            }
        }

        internal static void Prefix(GameWarProgress __instance)
        {
            activeWar = __instance;
            warMedals = 0;
        }

        internal static void RecordIncome(GameCurrency c, long amt)
        {
            if (activeWar != null && c == GameCurrency.Medal && amt > 0)
                warMedals += amt;
        }
    }
}
