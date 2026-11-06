using System;
using System.Linq;
using Assets.Behaviour.UI;
using Assets.Source.Player;
using BepInEx;
using HarmonyLib;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomGlobalLedger
{
    [BepInPlugin(Id, "Global Ledger", Version)]
    [BepInDependency("urakka.kingdominc.foundation", "0.6.0")]
    public sealed partial class GlobalLedgerPlugin : BaseUnityPlugin
    {
        public const string Id = "urakka.kingdominc.globalledger", Version = "0.1.4";
        internal static GlobalLedgerPlugin Instance;
        internal readonly LedgerBook Book = new LedgerBook(() => Time.realtimeSinceStartupAsDouble);
        internal LedgerPanel Panel;
        internal LedgerNavigation Navigation;
        internal IModBarRegistration Registration;
        Harmony hooks;
        GamePlayer player;
        internal static bool Diagnostic => Environment.GetCommandLineArgs().Contains("-global-ledger-smoke");

        void Awake()
        {
            Instance = this;
            try
            {
                hooks = new Harmony(Id);
                LedgerObserver.Install(hooks);
                hooks.CreateClassProcessor(typeof(HudVisibility)).Patch();
                hooks.CreateClassProcessor(typeof(EscapeDismiss)).Patch();
                Navigation = gameObject.AddComponent<LedgerNavigation>();
                Registration = FoundationApi.ModBar.Register(new LedgerBarElement(this));
                if (Diagnostic) { InstallDiagnostics(); Application.runInBackground = true; StartCoroutine(RuntimeCheck()); }
                Logger.LogInfo("Global Ledger prototype: Foundation Mod Bar, currency tree and live building activity ready.");
            }
            catch { Registration?.Dispose(); hooks?.UnpatchSelf(); Instance = null; throw; }
        }

        void Update() => EnsurePlayer();
        internal void EnsurePlayer()
        {
            if (ReferenceEquals(player, GamePlayer.Current)) return;
            player = GamePlayer.Current;
            Book.Reset();
            LedgerObserver.Reset();
            if (Navigation) Navigation.Clear();
            if (Panel) { Panel.ResetTree(); if (Panel.gameObject.activeSelf) Panel.ClosePopup(); }
        }
        void OnDestroy()
        {
            Registration?.Dispose();
            hooks?.UnpatchSelf();
            LedgerObserver.Reset();
            if (Navigation) Destroy(Navigation);
            if (Instance == this) Instance = null;
        }

        [HarmonyPatch(typeof(GameUI), nameof(GameUI.ToggleUI))]
        static class HudVisibility
        {
            static void Postfix(GameUI __instance)
            {
                var panel = Instance?.Panel;
                if (panel && panel.gameObject.activeSelf && !__instance.GetComponent<Canvas>().enabled) panel.ClosePopup();
            }
        }
        [HarmonyPatch(typeof(GameUI), nameof(GameUI.ProcessEscape))]
        static class EscapeDismiss
        {
            static bool Prefix(GameUI __instance)
            {
                var panel = Instance?.Panel;
                if (!panel || !panel.gameObject.activeSelf || !__instance.GetComponent<Canvas>().enabled || UIAlertWindow.Active || __instance.IsActive(GameUIType.Popup))
                    return true;
                panel.ClosePopup();
                return false;
            }
        }
    }

    internal sealed class LedgerBarElement : ModBarElement
    {
        readonly GlobalLedgerPlugin plugin;
        internal LedgerBarElement(GlobalLedgerPlugin plugin) : base(GlobalLedgerPlugin.Id, "open", 220, 0, ModBarAlignment.Right) { this.plugin = plugin; }
        public override IModBarView CreateView(IModBarContext context) => new LedgerBarView(plugin, context);
    }

    internal sealed class LedgerBarView : IModBarView
    {
        readonly GlobalLedgerPlugin plugin;
        readonly UnityEngine.UI.Button button;
        LedgerPanel panel;
        internal LedgerBarView(GlobalLedgerPlugin plugin, IModBarContext context)
        {
            this.plugin = plugin;
            button = context.CreateButton("Global Ledger v", Toggle);
        }
        void Toggle()
        {
            plugin.EnsurePlayer();
            if (GamePlayer.Current == null || !GameUI.Instance) return;
            if (!panel)
            {
                panel = LedgerPanel.Create(plugin.Book, button);
                plugin.Panel = panel;
                panel.OnPopupClosed += Refresh;
            }
            panel.TogglePopup();
            Refresh();
        }
        public void Refresh()
        {
            if (button) button.GetComponentInChildren<TMPro.TMP_Text>().text = panel && panel.gameObject.activeSelf ? "Global Ledger ^" : "Global Ledger v";
        }
        public void Dispose()
        {
            if (panel)
            {
                panel.OnPopupClosed -= Refresh;
                if (GameUI.Instance && panel.gameObject.activeSelf) panel.ClosePopup(false);
                UnityEngine.Object.Destroy(panel.gameObject);
            }
            if (plugin.Panel == panel) plugin.Panel = null;
        }
    }
}
