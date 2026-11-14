using System;
using System.Collections;
using System.IO;
using System.Linq;
using Assets.Source.Flow;
using Assets.Source.Player;
using BepInEx.Bootstrap;
using HarmonyLib;
using KingdomFoundation.Api;
using LightJson;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingdomGlobalLedger.Verification
{
    internal sealed class LedgerUiVerification
    {
        LedgerPanel panel;
        GameObject canvasRoot;
        GamePlayer original;
        JsonArray checks;
        string output;
        LedgerBookSource source;
        TMP_FontAsset nativeFont;
        internal bool Passed = true;
        internal string Failure;

        internal IEnumerator Run(string output, JsonArray checks)
        {
            this.output = output;
            this.checks = checks;
            if (!Try(Setup)) { Cleanup(); yield break; }
            yield return new WaitForSecondsRealtime(1);
            Try(VerifyBasic);
            Try(VerifyNameClipping);
            Try(() => Capture("v1-basic-1920.png"));
            yield return new WaitForSecondsRealtime(.5f);
            Try(VerifyAdvanced);
            Try(() => Capture("v1-production-1920.png"));
            yield return new WaitForSecondsRealtime(.5f);
            Try(VerifyControls);
            Try(VerifySmoothing);
            Try(VerifyRightClick);
            Try(() => { panel.SetSearch(""); panel.SetViewMode(LedgerPanel.ViewMode.Basic); panel.ToggleControls(); Capture("v1-collapsed-1920.png"); });
            yield return new WaitForSecondsRealtime(.5f);
            Try(() => { panel.ToggleControls(); Screen.SetResolution(1280, 720, false); });
            yield return new WaitForSecondsRealtime(1);
            Try(() => { Capture("v1-basic-1280.png"); VerifyFrameFits(); });
            yield return new WaitForSecondsRealtime(.5f);
            Cleanup();
        }

        bool Try(Action action)
        {
            try { action(); return true; }
            catch (Exception error) { Passed = false; Failure = (Failure ?? "") + error + "\n"; return false; }
        }

        void Check(bool value, string name)
        {
            if (!value) throw new InvalidOperationException(name);
            checks.Add(name);
        }

        void Setup()
        {
            Screen.SetResolution(1920, 1080, false);
            original = GamePlayer.Current;
            var player = new GamePlayer();
            var unlocked = (bool[])AccessTools.Field(typeof(GamePlayer), "_techUnlocked").GetValue(player);
            for (int i = 0; i < unlocked.Length; i++) unlocked[i] = true;
            player.AddResource((Assets.Source.Items.ResourceItemType)"iron_ore", 1);
            var flow = new FlowDiagram { Label = "Ironworks" };
            player.Flows.Add(flow);
            GamePlayer.Current = player;
            var plugin = GlobalLedgerPlugin.Instance;
            plugin.EnsurePlayer();
            plugin.EnableRollingAverage.Value = false;
            var node = new LedgerFixtureNode { Position = new Vector2Int(128, -128) };
            flow.Elements.Add(node);
            flow.Decorations.Add(new FlowDeco { Position = Vector2Int.zero, Size = new Vector2Int(512, 512) });
            flow.Decorations.Add(new FlowDeco { Label = "Ironworks", Position = new Vector2Int(10, -10), Size = new Vector2Int(64, 32) });
            source = new LedgerBookSource(node.Guid, "Fixture", "Workshop", "Workshop");
            plugin.Book.Record(source, GameCurrency.Gold, 10398, 710);
            plugin.Book.Record(source, GameCurrency.Mana, 125000, 25000);
            plugin.Book.Record(source, GameCurrency.Requisition, 6, 10);
            plugin.Book.RecordItem(source, "iron_ore", 100, 100);
            plugin.Book.RecordItem(source, "iron_ingot", 90, 40);
            plugin.Book.RecordItem(source, "iron_sword", 6, 2);

            var native = Resources.FindObjectsOfTypeAll<Button>().FirstOrDefault(button => button.GetComponent<Image>()?.sprite && button.GetComponentInChildren<TMP_Text>(true)?.font);
            if (!native) throw new InvalidOperationException("No native button/font loaded for pixel UI verification");
            nativeFont = native.GetComponentInChildren<TMP_Text>(true).font;
            File.WriteAllText(Path.Combine(output, "pixel-font.txt"), nativeFont.name);
            canvasRoot = new GameObject("LedgerPixelVerification", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var background = LedgerUi.Rect(canvasRoot.transform, "Backdrop");
            LedgerUi.Fill(background);
            background.gameObject.AddComponent<Image>().color = new Color(.07f, .07f, .06f);
            var anchor = LedgerUi.Rect(canvasRoot.transform, "Anchor");
            LedgerUi.Place(anchor, 20, 0, 220, 40);
            var image = anchor.gameObject.AddComponent<Image>();
            var nativeImage = native.GetComponent<Image>();
            image.sprite = nativeImage.sprite;
            image.type = nativeImage.type;
            image.color = nativeImage.color;
            image.sprite = Resources.FindObjectsOfTypeAll<Sprite>().First(sprite => sprite.name == "Button02");
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            var button = anchor.gameObject.AddComponent<Button>();
            var text = LedgerUi.Rect(anchor, "Label").gameObject.AddComponent<TextMeshProUGUI>();
            LedgerUi.Fill(text.rectTransform);
            text.font = nativeFont;
            text.fontSharedMaterial = native.GetComponentInChildren<TMP_Text>(true).fontSharedMaterial;
            text.text = "Global Ledger v";
            text.fontSize = 19;
            text.alignment = TextAlignmentOptions.Center;
            Canvas.ForceUpdateCanvases();
            panel = LedgerPanel.Create(plugin.Book, button);
            plugin.Panel = panel;
            panel.OpenPopup();
            panel.enabled = false; // The synthetic anchor has no real ModBar visibility lifecycle.
        }

        void VerifyBasic()
        {
            var ore = panel.VisibleRows.Single(row => row.Key == "basic/item/iron_ore");
            Check(ore.Income == 100 && ore.Outflow == 100 && ore.Amount == 0, "Balanced resources retain separate production and consumption in Basic");
            var mana = panel.VisibleRows.Single(row => row.Key == "basic/currency/4");
            Check(mana.Income == 125000 && mana.Outflow == 25000 && mana.Amount == 100000, "Basic currencies retain earned, spent and net totals");
            var rowRoot = panel.Scroller.content.Cast<Transform>().Single(child => child.name == mana.Key);
            Check(rowRoot.Find("In/Amount").GetComponent<TMP_Text>().text == "125,000", "Mana uses thousands separators in the actual rendered row");
            Check(rowRoot.Find("NameViewport/Name").GetComponent<TMP_Text>().font == nativeFont, "V1 retains the native pixel font");
            Check(panel.Frame.GetComponent<Image>().sprite?.name == "DefaultBorderDark", "V1 retains the native pixel frame");
            VerifyColumns();
            VerifyFrameFits();
        }

        void VerifyNameClipping()
        {
            var name = panel.Scroller.content.GetComponentsInChildren<LedgerNameScroll>().First();
            var label = name.GetComponentInChildren<TMP_Text>();
            name.enabled = false;
            try
            {
                name.Bind("long-name-regression", "Ironworks production and consumption in the eastern frontier workshop with a very long name");
                name.Layout();
                label.ForceMeshUpdate();
                Check(label.overflowMode == TextOverflowModes.Truncate && label.isTextTruncated, "Long pixel names clip without requesting the missing ellipsis glyph");
                name.OnPointerEnter(null);
                name.Advance(1.9f);
                Check(label.overflowMode == TextOverflowModes.Truncate, "Pixel names remain clipped during the hover delay");
                name.Advance(.6f);
                Check(label.overflowMode == TextOverflowModes.Overflow && label.rectTransform.anchoredPosition.x < 0,
                    "Long pixel names still scroll after the hover delay");
                name.OnPointerExit(null);
                label.ForceMeshUpdate();
                Check(label.overflowMode == TextOverflowModes.Truncate && label.rectTransform.anchoredPosition.x == 0,
                    "Leaving a name restores clipping without ellipsis warnings");
                Check(label.font == nativeFont, "Clipping and hover scrolling preserve the native pixel font");
            }
            finally { name.enabled = true; panel.RefreshNow(); }
        }

        void VerifyAdvanced()
        {
            panel.SetViewMode(LedgerPanel.ViewMode.Adv);
            panel.SelectTab(1);
            panel.ToggleRow("item/iron_ore");
            var container = panel.VisibleRows.Single(row => row.Depth == 1 && row.Item?.Identifier == "iron_ore");
            Check(container.Income == 100 && container.Outflow == 100 && container.Amount == 0, "Collapsed balanced containers expose production and consumption");
            Check(container.Target != null, "Container navigation targets survive the V2 cache backport");
            panel.SetSearch("Global Ledger fixture");
            Check(panel.VisibleRows.Any(row => row.Depth == 2), "Search finds building sources beneath collapsed parents");
            Check(panel.VisibleRows.Any(row => row.Depth == 0), "Search preserves resource ancestors of matching buildings");
            panel.SetSearch("no resource matches this phrase");
            Check(panel.VisibleRows.Count == 0, "Search removes unmatched hierarchy branches");
            panel.SetSearch("");
            VerifyColumns();
        }

        void VerifyControls()
        {
            panel.SetViewMode(LedgerPanel.ViewMode.Basic);
            int[][] windows = { new[] { 1, 10, 30 }, new[] { 60, 300, 600 }, new[] { 3600, 43200, 86400 } };
            for (int unit = 0; unit < windows.Length; unit++)
            {
                panel.SetTimeUnit((LedgerPanel.TimeUnit)unit);
                for (int option = 0; option < windows[unit].Length; option++)
                {
                    panel.SetWindow(option);
                    Check(panel.WindowSeconds == windows[unit][option], "Observation window " + windows[unit][option] + " seconds");
                }
            }
            Check(Math.Abs(panel.Rate(24) - 1) < .0001, "24-hour totals normalize correctly to an hourly rate");
            panel.SetTimeUnit(LedgerPanel.TimeUnit.Min);
            panel.SetWindow(2);
            Check(Math.Abs(panel.Rate(100) - 10) < .0001, "10-minute totals normalize correctly to a per-minute rate");
            panel.SetWindow(0);
            panel.ToggleFreeze();
            long before = panel.VisibleRows.Single(row => row.Key == "basic/currency/4").Income;
            GlobalLedgerPlugin.Instance.Book.Record(source, GameCurrency.Mana, 20, 0);
            panel.RefreshNow();
            Check(panel.VisibleRows.Single(row => row.Key == "basic/currency/4").Income == before, "Freeze retains the displayed snapshot while recording continues");
            panel.ToggleFreeze();
            Check(panel.VisibleRows.Single(row => row.Key == "basic/currency/4").Income == before + 20, "Resume refreshes the current accounting snapshot");
            panel.SetRefresh(10);
            panel.SetSort(3);
            panel.SetDescending(true);
            var items = panel.VisibleRows.Where(row => row.Item != null).ToArray();
            Check(items.First().Item.Identifier == "iron_ore", "Numeric consumption sorting puts the largest item consumer first");
            float oldHeight = panel.Frame.rect.height;
            panel.ToggleControls();
            Check(!panel.Frame.Find("Controls").gameObject.activeSelf && panel.Frame.rect.height < oldHeight, "Controls collapse and shrink the native panel");
            panel.ToggleControls();
            Check(panel.Frame.rect.height == oldHeight && panel.WindowSeconds == 60, "Restoring controls retains the selected window");
            var gold = LedgerUi.SplitGoldCompact(10398);
            Check(gold.Gold == 1 && gold.Silver == 4 && gold.Copper == 0, "Native coin display rounds only the displayed lower denomination");
            Check(LedgerUi.FormatAmount(125000.25) == "125,000.25", "Large rates retain grouped digits and meaningful decimal precision");
        }

        void VerifySmoothing()
        {
            var smoothing = new LedgerSmoothing();
            var row = new LedgerRowData("average", "Average", 0) { GameCurrency = GameCurrency.Mana, Income = 10, Outflow = 2 };
            smoothing.Apply(new[] { row }, 2, true);
            row.Income = 30; row.Outflow = 6;
            smoothing.Apply(new[] { row }, 2, true);
            Check(row.Income == 20 && row.Outflow == 4 && row.Amount == 16, "Running sums smooth production and consumption separately");
            row.Income = 50; row.Outflow = 10;
            smoothing.Apply(new[] { row }, 2, false);
            Check(row.Income == 20 && row.Outflow == 4, "UI-only refreshes do not inject duplicate average samples");
            row.Income = 50; row.Outflow = 10;
            smoothing.Apply(new[] { row }, 2, true);
            Check(row.Income == 40 && row.Outflow == 8, "Smoothing subtracts expired samples");
        }

        void VerifyRightClick()
        {
            Check(Chainloader.PluginInfos.ContainsKey("urakka.kingdominc.rightclickfix"), "Right Click Fix loaded in the isolated test runtime");
            var originalUi = Assets.Behaviour.UI.GameUI.Instance;
            var fixture = new GameObject("CancelUiFixture", typeof(RectTransform));
            fixture.SetActive(false);
            var gameUi = fixture.AddComponent<Assets.Behaviour.UI.GameUI>();
            var menu = new GameObject("IngameMenu", typeof(RectTransform));
            menu.transform.SetParent(fixture.transform);
            menu.SetActive(false);
            AccessTools.Field(gameUi.GetType(), "_ingameMenuContent").SetValue(gameUi, (RectTransform)menu.transform);
            var research = new GameObject("ResearchFixture");
            research.transform.SetParent(fixture.transform);
            research.SetActive(false);
            var researchField = AccessTools.Field(gameUi.GetType(), "_techMenu");
            researchField.SetValue(gameUi, research.AddComponent(researchField.FieldType));
            AccessTools.PropertySetter(gameUi.GetType(), "Instance").Invoke(null, new object[] { gameUi });
            bool cancelled = false;
            var subscription = FoundationApi.GameHooks.Subscribe("urakka.kingdominc.globalledger.verification", GameHookPoints.Cancel,
                after: call => cancelled = call.IsCancelled && !call.OriginalRan);
            try
            {
                gameUi.ProcessCancel();
                Check(cancelled && !panel.gameObject.activeSelf, "Native ProcessCancel with Right Click Fix dismisses V1 without running native cancellation");
            }
            finally
            {
                subscription.Dispose();
                AccessTools.PropertySetter(gameUi.GetType(), "Instance").Invoke(null, new object[] { originalUi });
                UnityEngine.Object.Destroy(fixture);
            }
            panel.OpenPopup();
            panel.enabled = false;
        }

        void VerifyColumns()
        {
            foreach (Transform child in panel.Scroller.content)
            {
                if (!child.gameObject.activeSelf) continue;
                foreach (string name in new[] { "In", "Out", "Net" })
                {
                    var cell = (RectTransform)child.Find(name);
                    if (!cell.gameObject.activeSelf) continue;
                    Check(cell.anchoredPosition.x + cell.rect.width <= LedgerPanel.ViewX, "Rate cell stays before the View button: " + name);
                }
            }
        }

        void VerifyFrameFits()
        {
            Canvas.ForceUpdateCanvases();
            panel.SendMessage("Fit");
            Canvas.ForceUpdateCanvases();
            Check(panel.Frame.localScale == Vector3.one, "Native pixel UI remains unscaled at " + Screen.width + "x" + Screen.height);
            var corners = new Vector3[4];
            panel.Frame.GetWorldCorners(corners);
            Check(corners.All(point => point.x >= -1 && point.y >= -1 && point.x <= Screen.width + 1 && point.y <= Screen.height + 1), "Native panel fits " + Screen.width + "x" + Screen.height);
        }

        void Capture(string name)
        {
            Canvas.ForceUpdateCanvases();
            panel.SendMessage("Fit");
            Canvas.ForceUpdateCanvases();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, name));
        }

        void Cleanup()
        {
            if (panel) UnityEngine.Object.Destroy(panel.gameObject);
            if (canvasRoot) UnityEngine.Object.Destroy(canvasRoot);
            GlobalLedgerPlugin.Instance.Panel = null;
            GamePlayer.Current = original;
            GlobalLedgerPlugin.Instance.EnsurePlayer();
        }
    }
}
