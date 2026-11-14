using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Player;
using Assets.Source.Util;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using KingdomFoundation.Api;
using LightJson;
using UnityEngine;

namespace KingdomGlobalLedger.Verification
{
    [BepInPlugin("urakka.kingdominc.globalledger.verification", "Global Ledger Verification", "0.1.13")]
    [BepInDependency(GlobalLedgerPlugin.Id)]
    public sealed class LedgerVerification : BaseUnityPlugin
    {
        readonly JsonArray checks = new JsonArray();
        readonly List<string> errors = new List<string>();
        readonly List<string> ellipsisWarnings = new List<string>();
        GlobalLedgerPlugin Plugin => GlobalLedgerPlugin.Instance;
        string output;

        void Awake()
        {
            if (!Environment.GetCommandLineArgs().Contains("-global-ledger-v1-verification")) { enabled = false; return; }
            output = Path.Combine(Paths.GameRootPath, "verification-results");
            Directory.CreateDirectory(output);
            Application.runInBackground = true;
            Application.logMessageReceived += Observe;
            var safety = new Harmony("urakka.kingdominc.globalledger.verification.safety");
            foreach (var name in new[] { "Store", "DoSave", "StoreAutosaveState" })
                safety.Patch(AccessTools.Method(typeof(SaveGame), name), prefix: new HarmonyMethod(GetType(), nameof(Skip)));
            safety.Patch(AccessTools.Method(typeof(SaveGame), "LoadLatestSave"), prefix: new HarmonyMethod(GetType(), nameof(False)));
            safety.Patch(AccessTools.Method(typeof(SteamManager), "Awake"), prefix: new HarmonyMethod(GetType(), nameof(Skip)));
            safety.Patch(AccessTools.PropertyGetter(typeof(SteamManager), "Initialized"), prefix: new HarmonyMethod(GetType(), nameof(False)));
            safety.Patch(AccessTools.Method(typeof(Assets.Behaviour.UI.MainMenu.MainMenuUI), "Update"), prefix: new HarmonyMethod(GetType(), nameof(Skip)));
            string saves = Path.Combine(output, "Saves");
            Directory.CreateDirectory(saves);
            SaveGame.SavesPath = saves;
            AccessTools.Field(typeof(SaveGame), "SavesDir").SetValue(null, new DirectoryInfo(saves));
            StartCoroutine(Run());
        }

        static bool Skip() => false;
        static bool False(ref bool __result) { __result = false; return false; }
        void Observe(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
                errors.Add(message + "\n" + stack);
            if (message.IndexOf("character used for Ellipsis", StringComparison.OrdinalIgnoreCase) >= 0)
                ellipsisWarnings.Add(message);
        }

        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(5);
            var result = new JsonObject();
            var original = GamePlayer.Current;
            try
            {
                Check(original == null, "No user kingdom loaded");
                Check(Chainloader.PluginInfos[GlobalLedgerPlugin.Id].Metadata.Version.ToString() == "0.1.13", "Global Ledger V1 0.1.13 loaded");
                VerifyAccounting();
                VerifyNativeCallbacks();
                VerifyLocations();
                VerifyBookWindows();
                result["passed"] = true;
            }
            catch (Exception error)
            {
                result["passed"] = false;
                result["failure"] = error.ToString();
            }
            finally
            {
                GamePlayer.Current = original;
                Plugin.EnsurePlayer();
            }
            if (Environment.GetCommandLineArgs().Contains("-ledger-visual-verification"))
            {
                var visuals = new LedgerUiVerification();
                yield return visuals.Run(output, checks);
                if (!visuals.Passed) { result["passed"] = false; result["visualFailure"] = visuals.Failure; }
            }
            result["checks"] = checks;
            result["errors"] = new JsonArray(errors.Select(error => (JsonValue)error).ToArray());
            result["ellipsisWarnings"] = new JsonArray(ellipsisWarnings.Select(warning => (JsonValue)warning).ToArray());
            result["saveFilesWritten"] = Directory.EnumerateFiles(SaveGame.SavesPath).Count();
            result["foundationVersion"] = typeof(FoundationApi).Assembly.GetName().Version.ToString();
            result["gameVersion"] = Application.version;
            if (errors.Count != 0 || ellipsisWarnings.Count != 0 || Directory.EnumerateFiles(SaveGame.SavesPath).Any()) result["passed"] = false;
            File.WriteAllText(Path.Combine(output, "results.json"), result.ToString());
            Application.logMessageReceived -= Observe;
            Application.Quit();
        }

        void Check(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException(name);
            checks.Add(name);
        }



        FlowDiagram NewWorld()
        {
            var player = new GamePlayer();
            var flow = new FlowDiagram { Label = "V2 verification" };
            player.Flows.Add(flow);
            GamePlayer.Current = player;
            Plugin.EnsurePlayer();
            return flow;
        }

        static GameWorkspace Wrap(FlowDiagram native) => (GameWorkspace)AccessTools.Method(typeof(GameWorkspace), "Wrap").Invoke(null, new object[] { native });
        static WorkspaceObject Identity(FlowElement element) => Wrap(element.ParentFlow).Buildings.First(value => value.Guid == element.Guid);

        void Operation(FlowElement element, Action action)
        {
            var identity = Identity(element);
            var current = LedgerObserver.Begin(identity);
            try { action(); }
            finally { LedgerObserver.End(current);  }
        }



        void VerifyAccounting()
        {
            var flow = NewWorld();
            var shop = new Shop { MaxGold = 10000 };
            flow.Elements.Add(shop);
            Operation(shop, () => shop.CollectedGold += 125);
            Check(Plugin.Book.Snapshot().Single().Earned[0] == 125, "Sale records uncollected till income");
            Operation(shop, () => { shop.CollectedGold -= 125; GamePlayer.Current.AddCurrency(Currency.Gold, 125, true); });
            Check(Plugin.Book.Snapshot().Single().Earned[0] == 125, "Collection transfers income without counting it twice");

            var machine = new LedgerFixtureNode();
            flow.Elements.Add(machine);
            GamePlayer.Current.AddCurrency(Currency.Mana, 1000, false);
            Operation(machine, () =>
            {
                GamePlayer.Current.AddCurrency(Currency.Mana, 50, true);
                Check(GamePlayer.Current.SpendCurrency(Currency.Mana, 20), "Successful spend exercised");
                Check(!GamePlayer.Current.SpendCurrency(Currency.Mana, 1000000), "Failed spend exercised");
            });
            var machineTotal = Plugin.Book.Snapshot().Single(total => total.Source.Id == machine.Guid);
            Check(machineTotal.Earned[4] == 50 && machineTotal.Spent[4] == 20, "Currency callbacks attribute successful operations to the active building");

            var producer = new LedgerFixtureNode { Kind = "DevSpawner" };
            flow.Elements.Add(producer);
            var item = new ItemType("ledger_v2_fixture_item");
            producer.Outputs[0].BufferedItems.Add(new InventoryItem(item));
            Operation(producer, () => producer.Outputs[0].BufferedItems.Add(new InventoryItem(item)));
            Check(Plugin.Book.Snapshot().Single(total => total.Source.Id == producer.Guid).ItemProduced(item.Identifier) == 1, "Only newly produced output items are counted");
            Operation(producer, () => { });

            var sold = new InventoryItem(item);
            shop.Inputs[0].BufferedItems.Add(sold);
            Operation(shop, () => shop.Inputs[0].ConsumeItem());
            Check(Plugin.Book.Snapshot().Single(total => total.Source.Id == shop.Guid).ItemConsumed(item.Identifier) == 1, "Input consumption callback records sold items");

            var quest = new LedgerFixtureNode { Kind = "QuestBoard" };
            flow.Elements.Add(quest);
            quest.Inputs[0].BufferedItems.Add(new InventoryItem(item));
            Operation(quest, () =>
            {
                var visitor = quest.Inputs[0].ConsumeItem();
                quest.Outputs[0].BufferedItems.Add(visitor);
                quest.Outputs[1].BufferedItems.Add(new InventoryItem(item));
            });
            var questTotal = Plugin.Book.Snapshot().Single(total => total.Source.Id == quest.Guid);
            Check(questTotal.ItemProduced(item.Identifier) == 1 && questTotal.ItemConsumed(item.Identifier) == 0, "Returning quest visitors are not counted as new production or consumption");

            Operation(machine, () => Operation(producer, () => GamePlayer.Current.AddCurrency(Currency.Mana, 7, true)));
            Check(LedgerObserver.current == null, "Nested operation scopes release correctly");
            Check(Plugin.Book.BatchWrites > 0 && LedgerObserver.RecordedBatches == Plugin.Book.BatchWrites, "Each active operation commits one accounting batch");
            var cachedSource = LedgerObserver.Describe(Identity(machine));
            Check(ReferenceEquals(cachedSource, LedgerObserver.Describe(Identity(machine))), "Unchanged building descriptions reuse cached metadata");
            machine.Position = new Vector2Int(900, -900);
            Plugin.Locations.Invalidate();
            Check(!ReferenceEquals(cachedSource, LedgerObserver.Describe(Identity(machine))), "Moved building descriptions refresh");
        }

        void VerifyNativeCallbacks()
        {
            var flow = NewWorld();
            var spawner = new DevSpawner { Item = new ItemType("ledger_v2_native_output") };
            flow.Elements.Add(spawner);
            TechNode tech = "dev_item_spawner";
            Check(tech != null, "Native spawner research exists");
            ((bool[])AccessTools.Field(typeof(GamePlayer), "_techUnlocked").GetValue(GamePlayer.Current))[tech.Ordinal] = true;
            spawner.Update(0.1f);
            Check(Plugin.Book.Snapshot().Single(total => total.Source.Id == spawner.Guid).ItemProduced(spawner.Item.Identifier) == 1, "Actual native spawner update reaches existing Foundation activity callbacks");
            var store = new Stockpile();
            flow.Elements.Add(store);
            store.Inputs[0].BufferedItems.Add(spawner.Outputs[0].ConsumeItem());
            long skipped = LedgerObserver.SkippedScopes;
            store.Update(0.1f);
            Check(LedgerObserver.SkippedScopes > skipped, "Actual native stockpile updates skip Global Ledger accounting snapshots");
            Check(!Plugin.Book.Snapshot().Any(total => total.Source.Id == store.Guid), "Storage and transport do not create production entries");
            var input = new ItemType("ledger_v2_crafting_input");
            var output = new ItemType("ledger_v2_crafting_output");
            var recipe = new CraftingRecipe("ledger_v2_crafting_recipe")
            {
                DisplayName = "V2 crafting fixture", CraftingTime = 1,
                Reagents = new[] { new CraftingReagent(input, 2) },
                Results = new[] { new CraftingResult(output, 2) }
            };
            var forge = new Forge();
            flow.Elements.Add(forge);
            forge.SetupRecipe(recipe);
            forge.Inputs[0].BufferedItems.Add(new InventoryItem(input));
            forge.Inputs[0].BufferedItems.Add(new InventoryItem(input));
            forge.Update(0.1f);
            Check(Plugin.Book.Snapshot().Single(total => total.Source.Id == forge.Guid).ItemConsumed(input.Identifier) == 2,
                "Actual native recipe start records both consumed reagents");
            forge.Progress = 1;
            forge.Update(0.1f);
            Check(Plugin.Book.Snapshot().Single(total => total.Source.Id == forge.Guid).ItemProduced(output.Identifier) == 2,
                "Actual native recipe completion records both produced outputs");
            recipe.Results[0].Enhancement = new ItemEnhancement("ledger_v2_enhancement_fixture");
            var enhancement = LedgerObserver.Begin(Identity(forge));
            Check(!enhancement.CaptureOutputs, "Changed enhancement recipes do not reuse stale output observation flags");
            LedgerObserver.End(enhancement);
            recipe.Results[0].Enhancement = null;
            var normalRecipe = LedgerObserver.Begin(Identity(forge));
            Check(normalRecipe.CaptureOutputs, "Returning to a production recipe restores output observation");
            LedgerObserver.End(normalRecipe);
        }

        LedgerBookTotals Total(FlowElement building)
            => new LedgerBookTotals(new LedgerBookSource(building.Guid, building.ElementType, "Fixture", "Fixture"));

        void VerifyLocations()
        {
            var flow = NewWorld();
            var building = new LedgerFixtureNode { Position = new Vector2Int(128, -128) };
            flow.Elements.Add(building);
            var outer = new FlowDeco { Position = Vector2Int.zero, Size = new Vector2Int(512, 512) };
            var label = new FlowDeco { Label = "Outer", Position = new Vector2Int(10, -10), Size = new Vector2Int(64, 32) };
            flow.Decorations.Add(outer); flow.Decorations.Add(label);
            double clock = 0;
            var locations = new LedgerLocations(() => clock);
            var subscriptions = new List<IDisposable>();
            locations.Install(subscriptions);
            try
            {
                var totals = new[] { Total(building) };
                var first = locations.Resolve(totals).Single();
                string key = first.GroupKey;
                Check(first.GroupName == "Outer", "Container names match the native layout");
                int scans = locations.DiscoveryCount, builds = locations.PlacementBuildCount;
                for (int i = 0; i < 100; i++) locations.Resolve(totals);
                Check(locations.DiscoveryCount == scans && locations.PlacementBuildCount == builds, "One hundred unchanged refreshes reuse the index");
                label.Label = "Renamed";
                FoundationApi.Events.Publish(new WorkspaceChangedEvent(WorkspaceChangeKind.Moved, flow, null, 0));
                Check(locations.Resolve(totals).Single().GroupName == "Renamed", "Existing Foundation workspace events invalidate cached locations");
                Check(locations.Resolve(totals).Single().GroupKey == key, "Container keys stay stable across cache rebuilds");
                var inner = new FlowDeco { Position = new Vector2Int(100, -100), Size = new Vector2Int(192, 192) };
                var innerLabel = new FlowDeco { Label = "Inner", Position = new Vector2Int(110, -110), Size = new Vector2Int(64, 32) };
                flow.Decorations.Add(inner); flow.Decorations.Add(innerLabel);
                clock += 2.1;
                Check(locations.Resolve(totals).Single().GroupName == "Inner", "Reconciliation catches unannounced container additions and selects the smallest frame");
                inner.Size = new Vector2Int(1, 1);
                clock += 2.1;
                Check(locations.Resolve(totals).Single().GroupName == "Renamed", "Reconciliation catches direct container resizing");
                building.Position = new Vector2Int(2000, -2000);
                clock += 2.1;
                Check(locations.Resolve(totals).Single().GroupName.Contains("Uncontained"), "Reconciliation catches direct building moves");
                flow.Elements.Remove(building);
                locations.Invalidate();
                Check(locations.Resolve(totals).Single().GroupKey == "removed", "Removed buildings retain their recorded totals in the removed group");
                flow.Elements.Add(building);
                var child = new FlowDiagram { Label = "Child" };
                var nested = new LedgerFixtureNode { InnerFlow = child };
                flow.Elements.Add(nested);
                child.Elements.Add(new LedgerFixtureNode { InnerFlow = flow });
                locations.Invalidate();
                Check(locations.AllFlows().Count == 2, "Nested workspace discovery handles cycles once");
                flow.Elements.Remove(nested);
                locations.Invalidate();
                Check(locations.AllFlows().Count == 1, "Removed nested workspaces leave the index");
                flow.Label = "New workspace name";
                clock += 2.1;
                Check(locations.Resolve(totals).Single().GroupName.StartsWith("New workspace name"), "Workspace label changes refresh uncontained groups");
                flow.Decorations.Clear();
                locations.Reset();
                Check(locations.Resolve(totals).Single().GroupName.Contains("Uncontained"), "Reset clears all stale container state");
            }
            finally { foreach (var hook in subscriptions) hook.Dispose(); }
        }

        void VerifyBookWindows()
        {
            double clock = 10;
            var book = new LedgerBook(() => clock);
            var source = new LedgerBookSource("fixture", "Fixture", "Fixture", "Fixture");
            book.Record(source, GameCurrency.Mana, 10, 3);
            book.RecordItem(source, "item", 4, 1);
            Check(book.Snapshot().Single().Net((int)GameCurrency.Mana) == 7, "Currency totals retain earned minus spent");
            Check(book.Snapshot().Single().ItemNet("item") == 3, "Item totals retain produced minus consumed");
            clock = 71;
            Check(book.Snapshot(60).Count == 0, "Expired activity leaves the display window");
            Check(book.Snapshot(1800).Count == 1, "Longer rolling windows retain older activity");
            clock = 1811;
            Check(book.Snapshot(1800).Count == 0, "Thirty-minute windows exclude older activity");
            Check(book.Snapshot(3600).Single().ItemNet("item") == 3, "One-hour window retains activity beyond thirty minutes");
            clock = 10 + 12 * 3600 - 1;
            Check(book.Snapshot(12 * 3600).Single().Net((int)GameCurrency.Mana) == 7, "Twelve-hour window retains its full history");
            clock = 10 + 24 * 3600 - 1;
            Check(book.Snapshot(24 * 3600).Single().ItemNet("item") == 3, "Twenty-four-hour window retains its full history");
            var recent = new LedgerBookSource("recent", "Fixture", "Fixture", "Recent");
            book.Record(recent, GameCurrency.Mana, 20, 5);
            Check(book.Snapshot(1).Single().Source.Id == "recent", "One-second windows select only recent buckets from a day of history");
            clock = 10 + 24 * 3600;
            Check(book.Snapshot(24 * 3600).Single().Source.Id == "recent", "Activity expires at the twenty-four-hour retention boundary");
            book.Reset();
            Check(!book.HasCurrencyActivity(4) && !book.HasItemActivity("item") && book.Snapshot(86400).Count == 0, "Session reset clears activity metadata and the time index");
        }




    }
}
