# Global Ledger

Track live currency earnings, operating expenses, and item production across your entire kingdom from the top Mod Bar in Kingdom Inc.

## Install and update

Install the requirements listed on the [Nexus page](https://www.nexusmods.com/kingdominc/mods/19) (**BepInEx 5** and **Foundation 0.6.0 or later**), then close Kingdom Inc and extract the ZIP into the game folder beside `KingdomInc.exe`. Confirm the installed file is at `BepInEx/plugins/GlobalLedger/GlobalLedger.dll` and launch the game.

## Features

- **Mod Bar dropdown**: Click **Global Ledger** on the right side of the shared Mod Bar beneath the currency bar to open or close the dropdown. While open, you can freely pan (WASD and mouse drag), zoom, and click on the workspace without closing the ledger.
- **Currencies tab**: View every unlocked currency's rolling net rate (`Earned - Spent` over the last 60 seconds). Gold is written out in full Gold, Silver, and Copper denominations with native coin symbols (`XX [Gold] XX [Silver] XX [Copper]`); other currencies retain their native units. Expand a currency root to inspect containers, individual buildings, and `Operating Costs` (icon-only currency rows).
- **Production tab**: View unlocked items (`Tier 1`–`Tier 12`, sorted **Tier → A–Z**) and their rolling net production rate (`Produced - Consumed` in `/min` over the last 60 seconds). Items with zero activity in the last minute are hidden by default; click **Show All** in the header bar to show or hide zero-activity unlocked items. Expand an item root to inspect containers, individual buildings, and `Operating Costs` (both consumed input items in `/min` and spent currencies as icon-only rows).
- **Container & workspace grouping**: Container rows display `[ContainerIcon] | [ProductionIcon] [ContainerName]` using the game's native container sprite, a `|` separator, and the overlapping icon sticker nearest the frame's top-left corner (or its building's production icon when no sticker is placed), alongside the cleaned container label (with TextMeshPro rich-text tags stripped). Buildings inside nested or overlapping frames belong to the smallest frame once; buildings outside frames appear under their workspace's `Uncontained` group.
- **Collapse All & independent expanders**: Click a row or its left arrow to expand or collapse its children without moving the camera. Click **Collapse All** in the header bar to collapse all open roots, containers, buildings, and operating cost groups on the active tab so you can drill down one level at a time.
- **Hover highlight & dedicated `View` button**: Hovering a container or building row highlights the target in the current workspace without moving the camera. Live container and building rows include a right-aligned **View** button that switches workspaces if needed, centers and highlights the target for five seconds, and closes the dropdown.

## How the accounting works

Rates reflect actual activity recorded in one-second buckets over the last 60 real elapsed seconds (`Earned - Spent` for currencies, `Produced - Consumed` for items). During the first minute of observation, the header shows a collecting indicator and displays the accumulated sample without extrapolating short bursts. Pausing the game produces no new activity while older observations age out.

Shop, Restaurant, Requisition Office, and Disenchanter collection-tray additions are combined with wallet credits so income is recognized when earned and collecting a tray never counts twice. Pure routing nodes and storage pass-throughs are excluded from item production and consumption. Deleted buildings keep their recorded observations under `Removed buildings` until the 60-second window expires.

## Saves and removal

Accounting history is session-local and adds no custom buildings, research, or save data. To remove the mod, close Kingdom Inc and delete `BepInEx/plugins/GlobalLedger`.

## Build from source

Keep the source checkout at `<KingdomInc>/_Mods/GlobalLedger` so project references resolve against your local game and BepInEx assemblies:

```powershell
dotnet build .\_Mods\GlobalLedger\GlobalLedger.csproj -c Release
.\_Mods\GlobalLedger\install.ps1
```

By rockyamyx / Urakka. Native game frames, fonts, and icons are used from the installed game and are not redistributed.

Source repository: https://github.com/urakkaamyx/KingdomInc_GlobalLedger
