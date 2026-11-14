using KingdomFoundation.Api;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    internal sealed class LedgerTreeRow
    {
        internal readonly RectTransform Root;
        internal readonly Button Button, ViewButton;
        readonly LedgerPanel owner;
        readonly TMP_Text label, arrow;
        readonly RectTransform nameViewport;
        readonly LedgerNameScroll nameScroll;
        readonly Image icon, prefixIcon, shade;
        readonly LedgerRowHover hover;
        readonly LedgerRateCell income, outflow, net;
        readonly int index;
        LedgerRowData data;

        internal LedgerTreeRow(LedgerPanel owner, LedgerUi ui, Transform parent, int index)
        {
            this.owner = owner;
            this.index = index;
            Root = LedgerUi.Rect(parent, "TreeRow" + index);
            shade = ui.Background(Root, Color.clear);
            Button = Root.gameObject.AddComponent<Button>();
            Button.targetGraphic = shade;
            Button.navigation = new Navigation { mode = Navigation.Mode.None };
            Button.onClick.AddListener(() =>
            {
                if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
                if (data == null) return;
                FoundationApi.UiAccess.PlaySound(GameUiSound.Button);
                owner.Activate(data);
            });
            hover = Root.gameObject.AddComponent<LedgerRowHover>();
            arrow = ui.Text(Root, "Expander", "", 17);
            arrow.color = LedgerUi.Gold;
            icon = CreateIcon("AssetIcon");
            prefixIcon = CreateIcon("ContainerIcon");
            nameViewport = LedgerUi.Rect(Root, "NameViewport");
            ui.Background(nameViewport, Color.clear);
            nameViewport.gameObject.AddComponent<RectMask2D>();
            label = ui.Text(nameViewport, "Name", "", 18);
            nameScroll = nameViewport.gameObject.AddComponent<LedgerNameScroll>();
            nameScroll.Initialize(label);
            income = new LedgerRateCell(ui, Root, "In");
            outflow = new LedgerRateCell(ui, Root, "Out");
            net = new LedgerRateCell(ui, Root, "Net");
            ViewButton = ui.Button(Root, "View", "View", () => { if (data?.Target != null) owner.View(data); }, 14);
        }

        Image CreateIcon(string name)
        {
            var image = LedgerUi.Rect(Root, name).gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        internal void Bind(LedgerRowData value, float width)
        {
            data = value;
            Root.name = value.Key;
            Root.gameObject.SetActive(true);
            nameScroll.Bind(value.Key, value.IconOnly ? "" : value.Label);
            label.color = value.IsHeader || value.Depth == 0 ? LedgerUi.Gold : LedgerUi.Paper;
            label.fontStyle = value.IsHeader ? FontStyles.Bold : FontStyles.Normal;
            shade.color = value.IsHeader ? new Color(.28f, .16f, .10f, .85f)
                : owner.SelectedKey == value.Key ? new Color(.35f, .22f, .13f, .7f)
                : new Color(0, 0, 0, index % 2 == 0 ? .13f : .03f);
            arrow.text = value.Expandable ? value.Open ? "v" : ">" : "";
            icon.sprite = value.Icon;
            icon.enabled = value.Icon && !value.IsHeader;
            prefixIcon.sprite = value.PrefixIcon;
            prefixIcon.enabled = value.PrefixIcon && !value.IsHeader;
            ViewButton.gameObject.SetActive(value.Target != null && !value.IsHeader);
            hover.Bind(value.Target);
            income.Bind(value, value.Income, false, owner, LedgerPanel.IncomeX);
            outflow.Bind(value, value.Outflow, false, owner, LedgerPanel.OutflowX);
            net.Bind(value, value.Amount, true, owner, LedgerPanel.NetX);
            Layout(width);
        }

        internal void Layout(float width)
        {
            if (data == null) return;
            LedgerUi.Place(Root, 0, data.Top, width, LedgerPanel.RowHeight - 1);
            if (data.IsHeader)
            {
                LedgerUi.Place(nameViewport, 16, 0, width - 32, LedgerPanel.RowHeight - 1);
                nameScroll.Layout();
                return;
            }
            float left = 8 + data.Depth * 22;
            LedgerUi.Place(arrow.rectTransform, left, 0, 20, 35);
            LedgerUi.Place(prefixIcon.rectTransform, left + 22, 8, 20, 20);
            float iconX = left + (prefixIcon.enabled ? 46 : 22);
            LedgerUi.Place(icon.rectTransform, iconX, 8, 20, 20);
            float textX = iconX + (icon.enabled ? 28 : 0);
            LedgerUi.Place(nameViewport, textX, 0, Mathf.Max(30, LedgerPanel.IncomeX - textX - 8), 35);
            nameScroll.Layout();
            LedgerUi.Place((RectTransform)ViewButton.transform, LedgerPanel.ViewX, 4, 42, 27);
        }
    }
}
