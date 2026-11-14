using BepInEx.Bootstrap;
using KingdomFoundation.Api;

namespace KingdomGlobalLedger
{
    internal static class LedgerCancelDismissPatch
    {
        internal static void Before(GameHookCall call)
        {
            if (call.IsCancelled || !Chainloader.PluginInfos.TryGetValue("urakka.kingdominc.rightclickfix", out var fix)
                || !fix.Instance || !fix.Instance.isActiveAndEnabled) return;
            var ui = FoundationApi.UiAccess;
            if (!ui.Root) return;
            if (ui.IsAlertVisible || ui.IsMenuVisible || ui.HasPopup || ui.IsDragDropBusy || ui.IsMenuOpen(GameMenu.Research)
                || ui.PointerTraverseDistance >= 50f || ui.PointerHasNativeRightClickAction) return;
            var panel = GlobalLedgerPlugin.Instance?.Panel;
            if (!panel || !panel.gameObject.activeSelf) return;
            panel.ClosePopup();
            call.Cancel();
        }
    }
}
