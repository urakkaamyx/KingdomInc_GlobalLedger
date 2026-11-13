using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using KingdomFoundation.Api;
using UnityEngine;

namespace KingdomGlobalLedger
{
    internal sealed class LedgerBarView : IModBarView
    {
        internal LedgerBarView(GlobalLedgerPlugin plugin, IModBarContext context)
        {
            this.plugin = plugin;
            button = context.CreateButton("Global Ledger v", Toggle);
        }

        readonly GlobalLedgerPlugin plugin;
        readonly UnityEngine.UI.Button button;
        LedgerPanel panel;
        public void Dispose()
        {
            if (panel)
            {
                panel.OnPopupClosed -= Refresh;
                if (FoundationApi.UiAccess.Root && panel.gameObject.activeSelf)
                    panel.ClosePopup(false);
                global::UnityEngine.Object.Destroy(panel.gameObject);
            }

            if (plugin.Panel == panel)
                plugin.Panel = null;
        }

        public void Refresh()
        {
            if (button)
                button.GetComponentInChildren<TMPro.TMP_Text>().text = panel && panel.gameObject.activeSelf ? "Global Ledger ^" : "Global Ledger v";
        }

        void Toggle()
        {
            plugin.EnsurePlayer();
            if (FoundationApi.Kingdom == null || !FoundationApi.UiAccess.Root)
                return;
            if (!panel)
            {
                panel = LedgerPanel.Create(plugin.Book, button);
                plugin.Panel = panel;
                panel.OnPopupClosed += Refresh;
            }

            panel.TogglePopup();
            Refresh();
        }
    }
}
