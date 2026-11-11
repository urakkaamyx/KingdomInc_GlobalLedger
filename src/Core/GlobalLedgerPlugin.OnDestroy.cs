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
        void OnDestroy()
        {
            Context?.Dispose();
            hooks?.UnpatchSelf();
            LedgerObserver.Reset();
            if (Navigation) Destroy(Navigation);
            if (Instance == this) Instance = null;
        }

    }
}
