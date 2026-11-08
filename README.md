# Global Ledger

Track live currency earnings, operating expenses, and item production across your entire kingdom from the top Mod Bar in Kingdom Inc.

## Install and update

Install the requirements listed on the [Nexus page](https://www.nexusmods.com/kingdominc/mods/19) (**BepInEx 5** and **Foundation 0.6.0 or later**), then close Kingdom Inc and extract the ZIP into the game folder beside `KingdomInc.exe`. Confirm the installed file is at `BepInEx/plugins/GlobalLedger/GlobalLedger.dll` and launch the game.

## Features

- **Rolling average UI button**: Toggle rolling averages on and off right from the top-right header bar via the `Rolling Avg` button (turns Gold when active) without opening the config menu.
- **Mod Config menu integration**: Configure rolling average damping options from the Mod Config menu:
  - **Rolling average**: Toggle to smooth out instantaneous spikes and jumpiness in rate calculations.
  - **Entry Count**: Slider (2 to 30, default 3) to configure how many entries to average. When `sec` is selected, averages over the chosen number of seconds; when `min` or `hour` is selected, averages over that many minutes.
- **Mod Bar dropdown**: Click **Global Ledger** on the right side of the shared Mod Bar beneath the currency bar to open or close the dropdown. While open, you can freely pan (WASD and mouse drag), zoom, and click on the workspace without closing the ledger.
- **Basic | Adv modes**: Toggle between Basic summary and Adv hierarchical view using the `Basic | Adv` buttons in the top-right corner.
  - **Basic mode**: Streamlined overview listing `Currency Net Collection` and `Item Net Production` (`[Icon] [Name] +/-XXX[icon] / {sec|min|hour}`), with items sorted by **Tier → A–Z**. Currencies and items without production activity in the window are cleanly omitted unless **Show All** is clicked.
  - **Adv mode**: Full hierarchical breakdown (`Currency / Item > Container > Building > Operating Costs`). Defaults to everything collapsed for clean drilling down.
- **Rate time unit selector**: Click `[sec | min | hour]` in the header bar (defaults to `min`) to dynamically scale all rates across both Basic and Adv modes.
- **Currencies tab (Adv)**: View every unlocked currency's rolling net rate (`Earned - Spent` over the last 60 seconds). Gold is written out in full Gold, Silver, and Copper denominations with native coin symbols (`XX [Gold] XX [Silver] XX [Copper]`); other currencies retain their native units. Expand a currency root to inspect containers, individual buildings, and `Operating Costs` (icon-only currency rows).
- **Production tab (Adv)**: View unlocked items (`Tier 1`–`Tier 12`, sorted **Tier → A–Z**) and their rolling net production rate (`Produced - Consumed`). Items with zero activity in the last minute are hidden by default; click **Show All** in the header bar to show or hide zero-activity unlocked items. Expand an item root to inspect containers, individual buildings, and `Operating Costs` (both consumed input items and spent currencies as icon-only rows).
- **Container & workspace grouping**: Container rows display `[ContainerIcon] | [ProductionIcon] [ContainerName]` using the game's native container sprite, a `|` separator, and the overlapping icon sticker nearest the frame's top-left corner (or its building's production icon when no sticker is placed), alongside the cleaned container label (with TextMeshPro rich-text tags stripped and duplicate names disambiguated with `_#`).
- **Default collapsed & independent expanders**: Hierarchical trees start collapsed by default. Click a row or its left arrow to expand or collapse its children without moving the camera.
- **Hover highlight & dedicated `View` button**: Hovering a container or building row highlights the target in the current workspace without moving the camera. Live container and building rows include a right-aligned **View** button that switches workspaces if needed, centers and highlights the target for five seconds, and keeps the ledger open.

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
