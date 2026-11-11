using System;
using System.Linq;
using Assets.Behaviour.UI;
using Assets.Source.Player;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomGlobalLedger
{
    public sealed partial class GlobalLedgerPlugin : BaseUnityPlugin
    {
        void Awake()
        {
            Instance = this;
            try
            {
                Context = FoundationApi.ForMod(Id);
                EnableRollingAverage = Config.Bind(
                    "Averages",
                    "EnableRollingAverage",
                    false,
                    "Checking this box enables Rolling average .. makes the calculations less jumpy but not as accurate realtime."
                );
                RollingAverageEntries = Config.Bind(
                    "Averages",
                    "RollingAverageEntries",
                    3,
                    "Number of entries to average when Rolling average is enabled."
                );
                RegisterModConfig();

                hooks = new Harmony(Id);
                LedgerObserver.Install(hooks);
                hooks.CreateClassProcessor(typeof(LedgerHudVisibilityPatch)).Patch();
                hooks.CreateClassProcessor(typeof(LedgerEscapeDismissPatch)).Patch();
                Navigation = gameObject.AddComponent<LedgerNavigation>();
                Context.Register(new LedgerBarElement(this));
            }
            catch { Context?.Dispose(); hooks?.UnpatchSelf(); Instance = null; throw; }
        }

    }
}
