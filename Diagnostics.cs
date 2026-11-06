using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Behaviour.UI;
using Assets.Behaviour.UI.Shared;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Flow.Stickers;
using Assets.Source.Items;
using Assets.Source.Player;
using Assets.Source.Util;
using BepInEx;
using HarmonyLib;
using KingdomFoundation.Api;
using LightJson;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KingdomGlobalLedger
{
    public sealed partial class GlobalLedgerPlugin
    {
        readonly List<string> checks = new List<string>(), exceptions = new List<string>();
        void Check(bool condition, string text) { if (!condition) throw new InvalidOperationException(text); checks.Add(text); }
        void Watch(string message, string stack, LogType kind) { if (kind == LogType.Exception) exceptions.Add(message + "\n" + stack); }
        Button BarButton() => GameUI.Instance.GetComponentsInChildren<Button>(true).Single(b => b.transform.parent.name == Id + ":open");
        Button RowButton(string key) => Panel.GetComponentsInChildren<Button>().Single(b => b.name == key);
        Button RowArrow(string key) => RowButton(key).transform.Find("Expander").GetComponent<Button>();
        Button RowView(string key) => RowButton(key).transform.Find("View").GetComponent<Button>();
        void RevealRow(string key)
        {
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)RowButton(key).transform;
            var contents = Panel.Scroller.content;
            float offset = Mathf.Clamp(-rect.anchoredPosition.y - 60, 0, Mathf.Max(0, contents.rect.height - Panel.Scroller.viewport.rect.height));
            contents.anchoredPosition = new Vector2(0, offset); Panel.Scroller.StopMovement(); Canvas.ForceUpdateCanvases();
        }
        LedgerPanel.RowData BuildingRow(FlowElement building, string prefix = "currency/0/") => Panel.VisibleRows.Single(r => r.Target?.Building == building && r.Key.StartsWith(prefix));
        LedgerPanel.RowData ContainerRow(FlowDeco container, string prefix = "currency/0/") => Panel.VisibleRows.Single(r => r.Target?.Container == container && r.Key.StartsWith(prefix));
        void PointerClick(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var canvas = button.GetComponentInParent<Canvas>().rootCanvas;
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 position = RectTransformUtility.WorldToScreenPoint(camera, button.transform.TransformPoint(((RectTransform)button.transform).rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = position, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button,
                "Actual pointer raycast reaches " + button.name + " at " + position + " hits=" + string.Join(",", hits.Take(5).Select(h => h.gameObject.name)));
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
        void InstallDiagnostics()
        {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach (var type in new[] { typeof(NoSave), typeof(NoAutosave), typeof(NoLoad), typeof(NoAchievement), typeof(Offline) }) hooks.CreateClassProcessor(type).Patch();
        }
        [HarmonyPatch(typeof(SaveGame), nameof(SaveGame.Store))] static class NoSave { static bool Prefix() => false; }
        [HarmonyPatch(typeof(SaveGame), nameof(SaveGame.StoreAutosaveState))] static class NoAutosave { static bool Prefix() => false; }
        [HarmonyPatch(typeof(SaveGame), nameof(SaveGame.LoadLatestSave))] static class NoLoad { static bool Prefix(ref bool __result) { __result = false; return false; } }
        [HarmonyPatch(typeof(SteamAchievement), nameof(SteamAchievement.Trigger))] static class NoAchievement { static bool Prefix() => false; }
        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Initialized), MethodType.Getter)] static class Offline { static bool Prefix(ref bool __result) { __result = false; return false; } }

        IEnumerator RuntimeCheck()
        {
            Application.logMessageReceived += Watch;
            string directory = Path.Combine(Paths.GameRootPath, "_Mods", "GlobalLedger", "Verification");
            Directory.CreateDirectory(directory);
            string error = null;
            Mine mineA = null, mineB = null;
            Forge forge = null;
            Collector collector = null;
            Enchanter enchanter = null;
            Shop shop = null;
            FlowDeco foundry = null, commerce = null, remoteContainer = null;
            Mine remoteMine = null;
            Mouse mouse = null;
            yield return new WaitForSecondsRealtime(4);
            try
            {
                Check(GamePlayer.Current == null && !GameUI.Instance, "Diagnostics start without opening a user kingdom");
                Check(!SaveGame.LoadLatestSave(), "Latest-save loading is disabled");
                double now = 100;
                var testBook = new LedgerBook(() => now);
                var source = new LedgerBook.Source("id", "Mine", "Mine", "Test mine");
                testBook.Record(source, Currency.Gold, 100, 20);
                testBook.RecordItem(source, "iron_ore", 5, 2); now += 59;
                Check(testBook.Snapshot().Single().Net(0) == 80 && testBook.Snapshot().Single().ItemNet("iron_ore") == 3,
                    "Rolling minute retains currency and item observations and subtracts spending/consumption");
                now += 1;
                Check(testBook.Snapshot().Count == 0, "Expired buckets leave the rolling minute without historical growth");
                testBook.Record(source, Currency.Gold, 0, 40); testBook.Record(source, Currency.Mana, 12, 3);
                testBook.RecordItem(source, "iron_ore", 0, 4); testBook.RecordItem(source, "iron_ingot", 2, 0);
                Check(testBook.Snapshot().Single().Net(0) == -40 && testBook.Snapshot().Single().Net(4) == 9
                    && testBook.Snapshot().Single().ItemNet("iron_ore") == -4 && testBook.Snapshot().Single().ItemNet("iron_ingot") == 2,
                    "Currencies and items never convert or offset across types");
                testBook.Reset(); Check(testBook.Snapshot().Count == 0, "Reset clears session observations");
                Check(LedgerUi.Rate(-12345, Currency.Gold) == "-1g 23s 45c" && LedgerUi.Rate(3, Currency.Mana) == "+3.00/min"
                    && LedgerUi.Rate(-1, Currency.Gold) == "-0g 0s 1c" && LedgerUi.Rate(0, Currency.Gold) == "0g 0s 0c"
                    && LedgerUi.ItemRate(3) == "+3.00/min" && LedgerUi.ItemRate(-2) == "-2.00/min" && LedgerUi.ItemRate(0) == "0.00/min",
                    "Gold rates use exact XXg XXs XXc denominations and other rates use two decimals");
                Check(LedgerLocations.CleanLabel("<size=48>01  FOREST DEFENSE</size>") == "01  FOREST DEFENSE",
                    "Container label cleaner strips rich-text size tags");
                var player = GamePlayer.CreateNewPlayer(); player.Flows[0].Elements.Clear(); player.Flows[0].Label = "Ironworks";
                player.AddResource("gold_ore", 1.0);
                player.AddTech("enchanter"); player.AddTech("requisition_office"); player.AddTech("shop");
                player.AddTech("blacksmith"); player.AddTech("iron_sword"); player.AddTech("collector");
                player.AddCurrency(Currency.Gold, 10000000, false); player.AddCurrency(Currency.Mana, 1000, false); player.AddCurrency(Currency.Requisition, 1000, false);
                player.AddCurrency(Currency.Ring, 10, false);
                Check(!Currency.Ring.IsUnlocked() && !ItemType.Get("sapphire").IsUnlocked,
                    "Currency and item visibility follow native unlock state rather than unresearched definitions");
                mineA = new Mine { Position = new Vector2Int(-600, 160) }; mineA.SetupResource("gold_ore");
                mineB = new Mine { Position = new Vector2Int(-600, -160) }; mineB.SetupResource("gold_ore");
                forge = new Forge { Position = new Vector2Int(-360, 0) }; forge.SetupRecipe("iron_ingot");
                collector = new Collector { Position = new Vector2Int(420, 0) };
                enchanter = new Enchanter { Position = new Vector2Int(200, -180) };
                shop = new Shop { Position = new Vector2Int(200, 180) };
                player.Flows[0].Elements.AddRange(new FlowElement[] { mineA, mineB, forge, collector, enchanter, shop });
                foundry = new FlowDeco { Position = new Vector2Int(-750, 300), Size = new Vector2Int(620, 800), IsParent = true };
                commerce = new FlowDeco { Position = new Vector2Int(100, 300), Size = new Vector2Int(640, 800), IsParent = true };
                player.Flows[0].Decorations.AddRange(new[] {
                    foundry, commerce,
                    new FlowDeco { Position = new Vector2Int(-730, 290), Size = new Vector2Int(250, 64), Label = "<size=48>A Foundry</size>", IsParent = true },
                    new FlowDeco { Position = new Vector2Int(-450, 290), Size = new Vector2Int(64, 64), Sticker = new IconSticker { IconName = "Icons_1" } },
                    new FlowDeco { Position = new Vector2Int(120, 290), Size = new Vector2Int(250, 64), Label = "B Commerce", IsParent = true }
                });
                var remoteFlow = new FlowDiagram { Label = "Exports" }; player.Flows.Add(remoteFlow);
                remoteMine = new Mine { Position = new Vector2Int(1600, 800) }; remoteMine.SetupResource("gold_ore"); remoteFlow.Elements.Add(remoteMine);
                remoteContainer = new FlowDeco { Position = new Vector2Int(1500, 1000), Size = new Vector2Int(700, 600), IsParent = true };
                remoteFlow.Decorations.AddRange(new[] { remoteContainer, new FlowDeco { Position = new Vector2Int(1520, 980), Size = new Vector2Int(250, 64), Label = "<size=48>C Exports</size>" } });
                EnsurePlayer(); SceneManager.LoadScene("Game");
            }
            catch (Exception ex) { error = ex.ToString(); }
            yield return new WaitForSecondsRealtime(4);
            if (error == null) try
            {
                mouse = InputSystem.AddDevice<Mouse>(); mouse.MakeCurrent();
                Check(FoundationApi.ModBar.Items.Count(r => r.ModId == Id) == 1
                    && FoundationApi.ModBar.Items.Single(r => r.ModId == Id).Element.Alignment == ModBarAlignment.Right,
                    "Exactly one Global Ledger entry is registered on the right of the Foundation Mod Bar");
                foreach (var element in GamePlayer.Current.Flows[0].Elements) element.UpdateUpgrades();
                Check(shop.World && mineA.World && forge.World, "Disposable fixture buildings have native world views");
                Book.Reset();
                long expectedCost = mineA.GetResourceCost().GetCurrency().Where(v => v.c == Currency.Gold).Sum(v => v.amt);
                mineA.DoManualCraft(); mineA.Progress = 1f; mineA.Update(0);
                mineB.DoManualCraft(); mineB.Progress = 1f; mineB.Update(0);
                Check(Book.Snapshot().Where(t => t.Source.TypeId == "Mine").Sum(t => t.Spent[0]) == expectedCost * 2,
                    "Real native manual resource orders record their actual costs once per building");
                var costFrame = LedgerObserver.Begin(enchanter);
                GamePlayer.Current.SpendCurrency(Currency.Gold, 15000); GamePlayer.Current.SpendCurrency(Currency.Mana, 5);
                Check(!GamePlayer.Current.SpendCurrency(Currency.Mana, long.MaxValue), "Unaffordable payment fails normally");
                LedgerObserver.End(costFrame);
                Check(Book.Snapshot().Single(t => t.Source.Id == enchanter.Guid).Spent[4] == 5, "Failed spending is excluded; all actual operating currencies are retained");
                shop.Inputs[0].BufferedItems.Add(new InventoryItem("iron_sword")); shop.Update(0);
                long sale = shop.CollectedGold;
                Check(sale > 0 && Book.Snapshot().Single(t => t.Source.Id == shop.Guid).Earned[0] == sale,
                    "Real shop sale is income when earned into the till (till=" + sale + ", cap=" + shop.MaxGold + ", recorded=" + Book.Snapshot().Where(t => t.Source.Id == shop.Guid).Sum(t => t.Earned[0]) + ")");
                shop.DoCollect(false);
                Check(Book.Snapshot().Single(t => t.Source.Id == shop.Guid).Earned[0] == sale, "Collecting that same till does not count income twice");
                Book.Reset(); shop.CollectedGold = 700; shop.DoCollect(false);
                Check(!Book.Snapshot().Any(t => t.Source.Id == shop.Guid), "Collecting a pre-existing till does not fabricate current income");
                mineA.DoManualCraft(); mineA.Progress = 1f; mineA.Update(0);
                mineB.DoManualCraft(); mineB.Progress = 1f; mineB.Update(0);
                int oreCount = forge.Recipe.Reagents[0].Count;
                for (int i = 0; i < oreCount; i++) forge.Inputs[0].BufferedItems.Add(new InventoryItem("iron_ore"));
                forge.Update(0); forge.Progress = 1f; forge.Update(0);
                collector.Inputs[0].BufferedItems.Add(new InventoryItem("iron_ore")); collector.Update(0);
                Check(!Book.Snapshot().Any(t => t.Source.Id == collector.Guid), "Pass-through routers do not record item production or consumption");
                costFrame = LedgerObserver.Begin(enchanter);
                GamePlayer.Current.SpendCurrency(Currency.Gold, 15000); GamePlayer.Current.SpendCurrency(Currency.Mana, 5);
                LedgerObserver.End(costFrame);
                shop.Inputs[0].BufferedItems.Add(new InventoryItem("iron_sword")); shop.Update(0);
                long before = Book.Snapshot().Sum(t => t.Net(0));
                GamePlayer.Current.SpendCurrency(Currency.Gold, 999);
                Check(Book.Snapshot().Sum(t => t.Net(0)) == before, "Non-building purchases and spending do not become operating costs");
                PointerClick(BarButton());
                Check(Panel && Panel.gameObject.activeSelf, "Foundation button opens the dropdown panel");
                Check(Panel.VisibleRows.Count(r => r.Depth == 0) == 3 && !Panel.VisibleRows.Any(r => r.Key == "currency/3"), "Only Gold, Requisition and Mana roots are visible; locked Rings stay hidden");
                Check(!RowView("currency/0").gameObject.activeSelf, "Root rows without a world target hide the View button");
                var gold = Panel.VisibleRows.Single(r => r.Key == "currency/0");
                Check(gold.Amount == Book.Snapshot().Sum(t => t.Net(0)), "Root net reconciles with the individual building nets");
                var goldRate = RowButton("currency/0").transform.Find("GoldRate");
                var (goldSign, goldG, goldS, goldC) = LedgerUi.SplitGold(gold.Amount);
                Check(goldRate.gameObject.activeSelf && !RowButton("currency/0").transform.Find("Rate").gameObject.activeSelf
                    && goldRate.Find("GoldAmount").GetComponent<TMP_Text>().text == goldSign + goldG
                    && goldRate.Find("SilverAmount").GetComponent<TMP_Text>().text == goldS.ToString()
                    && goldRate.Find("CopperAmount").GetComponent<TMP_Text>().text == goldC.ToString()
                    && goldRate.Find("GoldCoin").GetComponent<Image>().sprite == LedgerUi.GoldCoin()
                    && goldRate.Find("SilverCoin").GetComponent<Image>().sprite == LedgerUi.SilverCoin()
                    && goldRate.Find("CopperCoin").GetComponent<Image>().sprite == LedgerUi.CopperCoin()
                    && LedgerUi.GoldCoin() != null && LedgerUi.SilverCoin() != null && LedgerUi.CopperCoin() != null,
                    "Gold rows display XX[GoldCoin] XX[SilverCoin] XX[CopperCoin] with native coin symbols");
                var mines = ContainerRow(foundry);
                Check(mines.Label == "A Foundry" && mines.Amount == Book.Snapshot().Where(t => t.Source.TypeId == "Mine").Sum(t => t.Net(0)),
                    "Container label strips rich-text tags and groups two distinct Mine instances without a building-type layer");
                Check(RowView(mines.Key).gameObject.activeSelf && RowView(BuildingRow(mineA).Key).gameObject.activeSelf,
                    "Container and building rows with live targets show a dedicated View button on the right");
                Check(mines.PrefixIcon != null && mines.PrefixIcon == LedgerLocations.ContainerIcon() && mines.PrefixIcon != SpriteLibrary.Get("Icons_31")
                    && RowButton(mines.Key).transform.Find("IconSeparator").GetComponent<TMP_Text>().text == "|"
                    && mines.Icon == GamePlayer.Current.Flows[0].Decorations.Single(d => d.Sticker != null).Sticker.GetSprite(),
                    "Container rows display [ContainerIcon (" + mines.PrefixIcon?.name + ")] | [ProductionIcon] [ContainerName]");
                Check(BuildingRow(mineA).Icon == mineA.Icon && BuildingRow(shop).Icon == shop.Icon, "Building rows use their real native icons");
                var manaCost = Panel.VisibleRows.Single(r => r.Key == BuildingRow(enchanter).Key + "/costs/4");
                Check(manaCost.IconOnly && manaCost.Icon == Currency.Mana.GetIcon(), "Operating costs use resource icons with no redundant currency text");
                Capture(Path.Combine(directory, "global-ledger-initial.png"));
                PointerClick(RowButton(mines.Key));
                Check(Panel.gameObject.activeSelf && !Panel.VisibleRows.Any(r => r.Key.StartsWith(mines.Key + "/")),
                    "Clicking a container row itself collapses its children without navigating or closing the panel");
                PointerClick(RowButton(mines.Key));
                PointerClick(RowArrow(mines.Key));
                Check(!Panel.VisibleRows.Any(r => r.Key.StartsWith(mines.Key + "/")), "Container arrow collapses without moving the camera or closing the panel");
                PointerClick(RowArrow(mines.Key));
                PointerClick(RowButton("currency/0"));
                Check(!Panel.VisibleRows.Any(r => r.Key.StartsWith("currency/0/")), "Currency root collapses independently");
                PointerClick(RowButton("currency/0"));
                PointerClick(Panel.CollapseAllButton);
                Check(Panel.VisibleRows.All(r => r.Depth == 0 && !r.Open), "Collapse All button collapses all currency roots");
                PointerClick(RowButton("currency/0"));
                Check(Panel.VisibleRows.Where(r => r.Key.StartsWith("currency/0/")).All(r => r.Depth == 1 && !r.Open),
                    "Expanding a root after Collapse All reveals collapsed containers without dumping nested rows");
                Panel.ResetTree(); Panel.RefreshNow();
                PointerClick(Panel.ProductionButton);
                Check(Panel.SelectedTab == 1 && !Panel.ShowingAll, "Production tab activates with zero-production items hidden by default");
                Check(Panel.VisibleRows.Where(r => r.Depth == 0).Select(r => r.Key).SequenceEqual(new[] { "item/iron_ingot", "item/iron_ore", "item/iron_sword", "item/gold_ore" }),
                    "Production tab sorts active items by Tier then A-Z and hides zero-production items by default");
                PointerClick(Panel.ShowAllButton);
                Check(Panel.ShowingAll && Panel.VisibleRows.Any(r => r.Key == "item/iron_armor") && !Panel.VisibleRows.Any(r => r.Key == "item/sapphire"),
                    "Show All toggle reveals unlocked zero-production items while keeping unresearched items hidden");
                PointerClick(Panel.ShowAllButton);
                Check(!Panel.ShowingAll && !Panel.VisibleRows.Any(r => r.Key == "item/iron_armor"),
                    "Toggling Show All off hides zero-production items again");
                var goldOreRoot = Panel.VisibleRows.Single(r => r.Key == "item/gold_ore");
                var ironOreRoot = Panel.VisibleRows.Single(r => r.Key == "item/iron_ore");
                var ironIngotRoot = Panel.VisibleRows.Single(r => r.Key == "item/iron_ingot");
                var ironSwordRoot = Panel.VisibleRows.Single(r => r.Key == "item/iron_sword");
                Check(goldOreRoot.Amount == 2 && ironOreRoot.Amount == -oreCount && ironIngotRoot.Amount == 1 && ironSwordRoot.Amount == -1,
                    "Item root net production reconciles produced outputs minus consumed inputs across the kingdom");
                var ingotFoundry = ContainerRow(foundry, "item/iron_ingot/");
                var ingotForge = BuildingRow(forge, "item/iron_ingot/");
                var ingotOreCost = Panel.VisibleRows.Single(r => r.Key == ingotForge.Key + "/costs/item/iron_ore");
                Check(ingotFoundry.Label == "A Foundry" && ingotForge.Amount == 1 && ingotOreCost.IconOnly && ingotOreCost.Amount == -oreCount
                    && ingotOreCost.Icon == ItemType.Get("iron_ore").GetIcon(),
                    "Production building rows show consumed input items as icon-only operating costs");
                var goldOreMine = BuildingRow(mineA, "item/gold_ore/");
                var goldOreCost = Panel.VisibleRows.Single(r => r.Key == goldOreMine.Key + "/costs/currency/0");
                Check(goldOreCost.IconOnly && goldOreCost.Amount == -expectedCost && goldOreCost.Icon == Currency.Gold.GetIcon(),
                    "Production building rows also include spent currencies under Operating Costs");
                Capture(Path.Combine(directory, "global-ledger-production.png"));
                PointerClick(Panel.CollapseAllButton);
                Check(Panel.VisibleRows.All(r => r.Depth == 0 && !r.Open), "Collapse All button also collapses all item roots on the Production tab");
                Panel.ResetTree(); Panel.RefreshNow();
                PointerClick(Panel.CurrenciesButton);
                Canvas.ForceUpdateCanvases();
                Check(Panel.Scroller.content.rect.height > Panel.Scroller.viewport.rect.height, "Expanded tree has scrollable overflow");
                Panel.Scroller.verticalNormalizedPosition = .35f;
                Canvas.ForceUpdateCanvases(); float position = Panel.Scroller.content.anchoredPosition.y;
                Panel.RefreshNow(); Canvas.ForceUpdateCanvases();
                Check(Mathf.Abs(Panel.Scroller.content.anchoredPosition.y - position) < 1, "Live refresh preserves scroll position");
                Panel.Scroller.verticalNormalizedPosition = 1;
                Capture(Path.Combine(directory, "global-ledger-currencies.png"));
            }
            catch (Exception ex) { error = ex.ToString(); }
            if (error == null)
            {
                var navigationChecks = VerifyNavigation(mineA, mineB, remoteMine, foundry, remoteContainer, directory);
                while (error == null)
                {
                    bool more = false; object step = null;
                    try { more = navigationChecks.MoveNext(); if (more) step = navigationChecks.Current; }
                    catch (Exception ex) { error = ex.ToString(); }
                    if (!more || error != null) break;
                    yield return step;
                }
                (navigationChecks as IDisposable)?.Dispose();
            }
            if (error == null) try
            {
                GameUI.Instance.ProcessEscape();
                Check(!Panel.gameObject.activeSelf && !GameUI.MenuVisible, "Native Escape dismisses Ledger before opening the game menu");
                PointerClick(BarButton());
                var pointer = new PointerEventData(EventSystem.current) { position = new Vector2(200, 500), button = PointerEventData.InputButton.Left };
                var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
                Check(!hits.Any(h => h.gameObject.transform.IsChildOf(Panel.transform)) && Panel.gameObject.activeSelf,
                    "Workspace clicks outside Global Ledger are not intercepted and do not close the dropdown");
                WorldFlowCamera.Instance.RecalculateBounds();
                var camBefore = Camera.main.transform.position;
                int zoomBefore = WorldFlowCamera.Instance.ZoomLevel;
                Check(!UIBlocksZoom.Blocked, "Open Global Ledger does not block workspace zoom when cursor is over the workspace");
                WorldFlowCamera.Instance.Move(120, -80);
                var camMoved = Camera.main.transform.position;
                WorldFlowCamera.Instance.ChangeZoomLevel(zoomBefore > 0 ? -1 : 1);
                Check(Panel.gameObject.activeSelf && Vector2.Distance(camMoved, camBefore) > 10 && WorldFlowCamera.Instance.ZoomLevel != zoomBefore,
                    "Workspace camera pans and zooms normally while Global Ledger stays open");
                WorldFlowCamera.Instance.SetZoomLevel(zoomBefore);
                GameUI.Instance.ToggleUI(false);
                Check(!Panel.gameObject.activeSelf, "Hiding the HUD closes the custom dropdown");
                GameUI.Instance.ToggleUI(true);
            }
            catch (Exception ex) { error = ex.ToString(); }
            yield return null;
            if (error == null) try
            {
                PointerClick(BarButton());
                var pointer = new PointerEventData(EventSystem.current);
                var blocker = Panel.Frame.GetComponent<UIBlocksZoom>(); blocker.OnPointerEnter(pointer);
                Check(UIBlocksZoom.Blocked, "Dropdown hover blocks workspace zoom"); blocker.OnPointerExit(pointer);
                var canvas = BarButton().GetComponentInParent<Canvas>().rootCanvas;
                canvas.scaleFactor = 1.5f;
            }
            catch (Exception ex) { error = ex.ToString(); }
            yield return null;
            if (error == null) try
            {
                Panel.RefreshNow();
                var corners = new Vector3[4]; Panel.Frame.GetWorldCorners(corners);
                Check(corners.All(c => c.x >= -1 && c.x <= Screen.width + 1 && c.y >= -1 && c.y <= Screen.height + 1), "Dropdown fits the screen at enlarged HUD scale");
                Capture(Path.Combine(directory, "global-ledger-scaled.png"));
                BarButton().GetComponentInParent<Canvas>().rootCanvas.scaleFactor = 1;
                SceneManager.LoadScene("MainMenu");
            }
            catch (Exception ex) { error = ex.ToString(); }
            yield return new WaitForSecondsRealtime(2);
            if (error == null) try
            {
                Check(!Panel && Registration.IsRegistered, "Scene teardown destroys the custom popup while retaining its Foundation registration");
                Check(!FindObjectsByType<LedgerTargetOutline>(FindObjectsSortMode.None).Any(), "Scene teardown leaves no stale building or container outlines");
                GamePlayer.CreateNewPlayer(); EnsurePlayer();
                Check(Book.Snapshot().Count == 0, "A different kingdom clears the previous kingdom's financial history");
                SceneManager.LoadScene("Game");
            }
            catch (Exception ex) { error = ex.ToString(); }
            yield return new WaitForSecondsRealtime(3);
            if (error == null) try
            {
                PointerClick(BarButton());
                Check(Panel && Panel.VisibleRows.Count(r => r.Depth == 0) == 1 && FoundationApi.ModBar.Items.Count(r => r.ModId == Id) == 1,
                    "A fresh HUD recreates one working button and shows only the new kingdom's unlocked currency");
                Check(exceptions.Count == 0, "No Unity exceptions during the prototype checks");
            }
            catch (Exception ex) { error = ex.ToString(); }
            var report = new JsonObject {
                {"passed", error == null}, {"version", Version}, {"gameVersion", Application.version}, {"unityVersion", Application.unityVersion},
                {"checks", new JsonArray(checks.Select(c => (JsonValue)c).ToArray())}, {"count", checks.Count},
                {"error", error ?? ""}, {"exceptions", new JsonArray(exceptions.Select(c => (JsonValue)c).ToArray())},
                {"dllSha256", HashFile(typeof(GlobalLedgerPlugin).Assembly.Location)},
                {"plugins", new JsonArray(BepInEx.Bootstrap.Chainloader.PluginInfos.Values.Select(p => (JsonValue)(p.Metadata.Name + " " + p.Metadata.Version)).ToArray())}
            };
            File.WriteAllText(Path.Combine(directory, "runtime-tests.json"), report.ToString());
            Application.logMessageReceived -= Watch;
            if (error != null) Logger.LogError(error); else Logger.LogInfo("Global Ledger passed " + checks.Count + " runtime checks.");
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            Application.Quit();
        }
        static string HashFile(string path)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        // Adapted from Foundation's isolated capture helper; only game-owned artwork is referenced.
        static void Capture(string path)
        {
            var camera = Camera.main;
            Canvas.ForceUpdateCanvases();
            var overlays = FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            var cameras = overlays.Select(c => c.worldCamera).ToArray(); var distances = overlays.Select(c => c.planeDistance).ToArray();
            var orders = overlays.Select(c => c.sortingOrder).ToArray();
            var target = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);
            var previous = RenderTexture.active; var oldTarget = camera.targetTexture; int oldMask = camera.cullingMask;
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            try
            {
                foreach (var canvas in overlays) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10; canvas.sortingOrder = 30000; }
                camera.cullingMask = -1; camera.targetTexture = target; Canvas.ForceUpdateCanvases(); camera.Render();
                RenderTexture.active = target; texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0); texture.Apply();
                File.WriteAllBytes(path, ImageConversion.EncodeToPNG(texture));
            }
            finally
            {
                camera.targetTexture = oldTarget; camera.cullingMask = oldMask;
                for (int i = 0; i < overlays.Length; i++) { overlays[i].renderMode = RenderMode.ScreenSpaceOverlay; overlays[i].worldCamera = cameras[i]; overlays[i].planeDistance = distances[i]; overlays[i].sortingOrder = orders[i]; }
                Canvas.ForceUpdateCanvases(); RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); Destroy(texture);
            }
        }
    }
}
