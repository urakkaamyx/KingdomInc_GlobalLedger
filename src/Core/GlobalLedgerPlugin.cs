using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomGlobalLedger
{
    [BepInPlugin(Id, "Global Ledger", Version)]
    [BepInDependency("urakka.kingdominc.foundation", "0.14.2")]
    public sealed class GlobalLedgerPlugin : BaseUnityPlugin
    {
        void Awake()
        {
            Instance = this;
            try
            {
                Context = FoundationApi.ForMod(Id);
                EnableRollingAverage = Config.Bind("Averages", "EnableRollingAverage", false, "Smooth displayed rates over recent refresh samples. The selected observation window is independent.");
                RollingAverageEntries = Config.Bind("Averages", "RollingAverageEntries", 3, "Number of display refresh samples to average (2 to 30).");
                RegisterModConfig();
                LedgerObserver.Install(hooks);
                Locations.Install(hooks);
                hooks.Add(FoundationApi.GameHooks.Subscribe(Id, GameHookPoints.Cancel, before: LedgerCancelDismissPatch.Before, priority: 100));
                hooks.Add(FoundationApi.GameHooks.Subscribe(Id, GameHookPoints.UiVisibility, after: call => LedgerHudVisibilityPatch.Postfix(call.View)));
                hooks.Add(FoundationApi.GameHooks.Subscribe(Id, GameHookPoints.Escape, before: call =>
                {
                    if (!LedgerEscapeDismissPatch.Prefix(call.View))
                        call.Cancel();
                }));
                Navigation = gameObject.AddComponent<LedgerNavigation>();
                Context.Register(new LedgerBarElement(this));
            }
            catch
            {
                Context?.Dispose();
                ReleaseHooks();
                Instance = null;
                throw;
            }
        }

        public const string Id = "urakka.kingdominc.globalledger", Version = "0.1.13";
        internal static GlobalLedgerPlugin Instance;
        internal readonly LedgerBook Book = new LedgerBook(() => Time.realtimeSinceStartupAsDouble);
        internal readonly LedgerLocations Locations = new LedgerLocations();
        internal LedgerPanel Panel;
        internal LedgerNavigation Navigation;
        internal FoundationModContext Context;
        internal ConfigEntry<bool> EnableRollingAverage;
        internal ConfigEntry<int> RollingAverageEntries;
        System.Collections.Generic.List<IDisposable> hooks = new System.Collections.Generic.List<IDisposable>();
        GameKingdom player;
        void ReleaseHooks()
        {
            foreach (var hook in hooks)
                hook.Dispose();
            hooks.Clear();
        }

        internal void EnsurePlayer()
        {
            if (ReferenceEquals(player, FoundationApi.Kingdom))
                return;
            player = FoundationApi.Kingdom;
            Book.Reset();
            Locations.Reset();
            LedgerObserver.Reset();
            if (Navigation)
                Navigation.Clear();
            if (Panel)
            {
                Panel.ResetTree();
                if (Panel.gameObject.activeSelf)
                    Panel.ClosePopup();
            }
        }

        void OnDestroy()
        {
            Context?.Dispose();
            ReleaseHooks();
            LedgerObserver.Reset();
            if (Navigation)
                Destroy(Navigation);
            if (Instance == this)
                Instance = null;
        }

        void RegisterModConfig()
        {
            var section = Context.Settings("Global Ledger", "Tracks live currency earnings, operating expenses, and item production across your entire kingdom.");
            section.ClearElements();
            section.AddToggle(EnableRollingAverage, "Rolling average");
            section.AddText("Smooth rates over recent refresh samples. Choose the observation window in the ledger; longer windows help with dungeon rewards.");
            section.AddSlider(RollingAverageEntries, 2, 30, "Entry Count");
        }

        void Update() => EnsurePlayer();
    }
}
