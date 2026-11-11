using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.Shared;
using Assets.Behaviour.UI.TechTree;
using Assets.Source.Items;
using Assets.Source.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    internal sealed partial class LedgerTreeRow
    {
        internal LedgerTreeRow(LedgerPanel owner, LedgerUi ui, Transform parent, int index)
        {
            this.owner = owner;
            this.index = index;
            Root = LedgerUi.Rect(parent, "TreeRow" + index);
            shade = ui.Background(Root, Color.clear);
            Button = Root.gameObject.AddComponent<Button>(); Button.targetGraphic = shade;
            Button.navigation = new Navigation { mode = Navigation.Mode.None };
            Button.onClick.AddListener(() => {
                if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
                if (data != null) { UISounds.Button(); owner.Activate(data); }
            });
            hover = Root.gameObject.AddComponent<LedgerRowHover>();
            var arrowRect = LedgerUi.Rect(Root, "Expander");
            var arrowHit = ui.Background(arrowRect, Color.clear);
            expander = arrowRect.gameObject.AddComponent<Button>(); expander.targetGraphic = arrowHit;
            expander.navigation = new Navigation { mode = Navigation.Mode.None };
            expander.onClick.AddListener(() => {
                if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
                if (data?.Expandable == true) { UISounds.Button(); owner.ToggleRow(data.Key); }
            });
            arrow = ui.Text(arrowRect, "Arrow", "", 17);
            prefixIcon = LedgerUi.Rect(Root, "ContainerIcon").gameObject.AddComponent<Image>(); prefixIcon.preserveAspect = true; prefixIcon.raycastTarget = false;
            separator = ui.Text(Root, "IconSeparator", "", 17); separator.alignment = TextAlignmentOptions.Center; separator.color = LedgerUi.Muted;
            icon = LedgerUi.Rect(Root, "CurrencyIcon").gameObject.AddComponent<Image>(); icon.preserveAspect = true; icon.raycastTarget = false;
            label = ui.Text(Root, "Name", ""); amount = ui.Text(Root, "Rate", ""); amount.alignment = TextAlignmentOptions.MidlineRight;
            amount.enableAutoSizing = true; amount.fontSizeMin = 13; amount.fontSizeMax = 19;
            goldGroup = LedgerUi.Rect(Root, "GoldRate");
            goldText = CoinText(ui, goldGroup, "GoldAmount"); goldCoin = CoinIcon(goldGroup, "GoldCoin");
            silverText = CoinText(ui, goldGroup, "SilverAmount"); silverCoin = CoinIcon(goldGroup, "SilverCoin");
            copperText = CoinText(ui, goldGroup, "CopperAmount"); copperCoin = CoinIcon(goldGroup, "CopperCoin");
            goldUnit = ui.Text(goldGroup, "GoldUnit", "", 13); goldUnit.color = LedgerUi.Muted; goldUnit.alignment = TextAlignmentOptions.MidlineLeft;
            ViewButton = ui.Button(Root, "View", "View", () => { if (data?.Target != null) owner.View(data); }, 14);
        }

    }
}
