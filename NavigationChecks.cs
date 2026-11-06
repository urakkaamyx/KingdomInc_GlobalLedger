using System;
using System.Collections;
using System.IO;
using System.Linq;
using Assets.Behaviour.Flow;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Player;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KingdomGlobalLedger
{
    public sealed partial class GlobalLedgerPlugin
    {
        IEnumerator VerifyNavigation(Mine mineA, Mine mineB, Mine remoteMine, FlowDeco foundry, FlowDeco remoteContainer, string directory)
        {
                var mineRow = BuildingRow(mineA); RevealRow(mineRow.Key);
                var hoverEvent = new PointerEventData(EventSystem.current);
                var camera = Camera.main; var beforeHover = camera.transform.position;
                string beforeJson = GamePlayer.Current.ActiveFlow.ToJson().ToString();
                ExecuteEvents.Execute(RowButton(mineRow.Key).gameObject, hoverEvent, ExecuteEvents.pointerEnterHandler);
                Check(Navigation.Highlighted?.Building == mineA && camera.transform.position == beforeHover,
                    "Hovering a building row highlights that building without moving the camera");
                Check(beforeJson == GamePlayer.Current.ActiveFlow.ToJson().ToString(), "Hover highlighting does not change the saved graph");
                ExecuteEvents.Execute(RowButton(mineRow.Key).gameObject, hoverEvent, ExecuteEvents.pointerExitHandler);
                Check(Navigation.Highlighted == null, "Leaving a building row clears its transient highlight");
                PointerClick(RowButton(mineRow.Key));
                Check(Panel.gameObject.activeSelf && camera.transform.position == beforeHover && !Panel.VisibleRows.Any(r => r.Key.StartsWith(mineRow.Key + "/")),
                    "Clicking a building row itself only toggles its expansion without moving the camera or closing the dropdown");
                PointerClick(RowButton(mineRow.Key));
                PointerClick(RowView(mineRow.Key));
                yield return null;
                var center = mineA.World.transform.TransformPoint(((RectTransform)mineA.World.transform).rect.center);
                Check(Panel.gameObject.activeSelf && Vector2.Distance(camera.transform.position, center) < .1f && Navigation.Highlighted?.Building == mineA,
                    "Building View button centers the actual building and highlights it without closing the dropdown");
                Check(!mineA.World.GetComponentInChildren<LedgerTargetOutline>().raycastTarget, "Highlights cannot intercept native world controls");
                Capture(Path.Combine(directory, "global-ledger-building-focus.png"));
                Navigation.Clear();
                yield return null;
                var frameRow = ContainerRow(foundry); RevealRow(frameRow.Key); beforeHover = camera.transform.position;
                ExecuteEvents.Execute(RowButton(frameRow.Key).gameObject, hoverEvent, ExecuteEvents.pointerEnterHandler);
                Check(Navigation.Highlighted?.Container == foundry && camera.transform.position == beforeHover, "Hovering a container row highlights its frame without moving the camera");
                ExecuteEvents.Execute(RowButton(frameRow.Key).gameObject, hoverEvent, ExecuteEvents.pointerExitHandler);
                Check(Navigation.Highlighted == null, "Leaving a container row clears its transient highlight");
                PointerClick(RowView(frameRow.Key));
                yield return null;
                center = foundry.World.transform.TransformPoint(((RectTransform)foundry.World.transform).rect.center);
                Check(Panel.gameObject.activeSelf && Vector2.Distance(camera.transform.position, center) < .1f && Navigation.Highlighted?.Container == foundry,
                    "Container View button centers and highlights the actual container without closing the dropdown");
                Capture(Path.Combine(directory, "global-ledger-container-focus.png"));
                Navigation.Clear();
                yield return null;
                var beforeMove = Book.Snapshot().Sum(t => t.Net(0));
                var originalPosition = foundry.Position;
                foundry.Position += new Vector2Int(4000, 0); Panel.RefreshNow();
                Check(BuildingRow(mineA).Key.Contains("uncontained/") && Panel.VisibleRows.Single(r => r.Key == "currency/0").Amount == beforeMove,
                    "Moving a container updates membership without losing or double-counting historical amounts");
                foundry.Position = originalPosition; Panel.RefreshNow();
                var outer = new FlowDeco { Position = new Vector2Int(-1000, 600), Size = new Vector2Int(3000, 2000) };
                GamePlayer.Current.ActiveFlow.Decorations.Add(outer); Panel.RefreshNow();
                Check(Panel.VisibleRows.Count(r => r.Target?.Building == mineA && r.Key.StartsWith("currency/0/")) == 1 && BuildingRow(mineA).Key.StartsWith(ContainerRow(foundry).Key + "/"),
                    "Overlapping containers choose the smallest frame and list each building once");
                GamePlayer.Current.ActiveFlow.Decorations.Remove(outer);
                GamePlayer.Current.ActiveFlow.Elements.Remove(mineB); Panel.RefreshNow();
                var removedBuildingRow = Panel.VisibleRows.SingleOrDefault(r => r.Key.StartsWith("currency/0/removed/") && r.Target == null && r.Depth == 2);
                Check(removedBuildingRow != null && !RowView(removedBuildingRow.Key).gameObject.activeSelf,
                    "Removed buildings retain their recent activity and hide the View button");
                GamePlayer.Current.ActiveFlow.Elements.Add(mineB); Panel.RefreshNow();
                remoteMine.UpdateUpgrades(); remoteMine.DoManualCraft(); Panel.RefreshNow();
                var remoteRow = BuildingRow(remoteMine); RevealRow(remoteRow.Key);
                yield return null;
                beforeHover = camera.transform.position;
                ExecuteEvents.Execute(RowButton(remoteRow.Key).gameObject, hoverEvent, ExecuteEvents.pointerEnterHandler);
                Check(GamePlayer.Current.CurrentFlow == 0 && camera.transform.position == beforeHover, "Hovering a building in another workspace never switches workspace or pans");
                ExecuteEvents.Execute(RowButton(remoteRow.Key).gameObject, hoverEvent, ExecuteEvents.pointerExitHandler);
                PointerClick(RowView(remoteRow.Key));
                yield return null;
                center = remoteMine.World.transform.TransformPoint(((RectTransform)remoteMine.World.transform).rect.center);
                Check(Panel.gameObject.activeSelf && GamePlayer.Current.CurrentFlow == 1 && Vector2.Distance(camera.transform.position, center) < .1f && Navigation.Highlighted?.Building == remoteMine,
                    "Clicking View on a building in another workspace switches there, centers it and highlights it without closing the dropdown");
                Navigation.Clear(); WorldFlowManager.Instance.ShowFlow(0);
                yield return null;
                var remoteFrameRow = ContainerRow(remoteContainer); RevealRow(remoteFrameRow.Key);
                yield return null;
                Check(remoteFrameRow.Label == "C Exports", "Remote container label also strips rich-text size tags");
                PointerClick(RowView(remoteFrameRow.Key));
                yield return null;
                Check(Panel.gameObject.activeSelf && GamePlayer.Current.CurrentFlow == 1 && Navigation.Highlighted?.Container == remoteContainer,
                    "Container navigation also works across workspaces without closing the dropdown");
                Navigation.Clear(); WorldFlowManager.Instance.ShowFlow(0);
                yield return null; Panel.Scroller.verticalNormalizedPosition = 1;

        }
    }
}
