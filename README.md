# Global Ledger

**Global Ledger 0.1.13 — for Kingdom Inc game version 1.0.14.**

[GitHub releases](https://github.com/KingdomIncModding/GlobalLedger/releases) · [Nexus Mods](https://www.nexusmods.com/kingdominc/mods/19)

Track live currency earnings, operating expenses, and item production across your entire kingdom from a dropdown on the top Mod Bar in Kingdom Inc.

Monitor economy flow in real time without placing physical tracking nodes in every factory line.

## Features

- **Top Mod Bar Dropdown:** Access real-time financial and production data from the top Mod Bar at any time.
- **Basic and Advanced Modes:**
  - **Basic Mode:** Scrollable currency and item overview with separate In, Out and Net columns.
  - **Advanced Mode:** Full hierarchical drill-down by Currency / Item > Container > Building > Operating Costs.
- **Original Pixel Look:** Native pixel font, frames, buttons, resource icons and coin artwork.
- **Observation Windows:** Choose 1s / 10s / 30s, 1m / 5m / 10m, or 1h / 12h / 24h. Rates follow the selected unit and window.
- **Separate In / Out / Net:** See production and consumption even when a container balances to zero.
- **Search and Sorting:** Find resources, containers and buildings, including sources under collapsed parents. Sort within each section or hierarchy level by name, tier, income, outflow or net. Click a numeric heading to sort it; click again to reverse the order.
- **Refresh and Freeze:** Cycle the refresh interval from 1 to 10 seconds. Freeze the display while activity continues to be recorded, then Resume to update it.
- **Configurable Rolling Averages:** Smooth displayed rates over 2–30 refresh samples. The observation window stays independent of smoothing.
- **Collapsible Controls:** Hide the controls to make the window smaller without losing the selected settings.
- **Multi-Currency Accounting:** Gold, Silver, and Copper use native denominations; Requisition, Mana, Crowns, Medals, and Rings maintain their own units.
- **Container & Workspace Grouping:** Automatically groups production by blueprint containers and workspaces, with container icon stickers and clean names.
- **Live View Navigation:** Click the View button on any container or building row to switch workspaces, pan the camera, and highlight the building.
- **Readable Values:** Thousands separators for large amounts, compact native coin denominations and scrolling long names after a two-second hover.
- **Right Click Fix Integration:** With Right Click Fix active, a short right-click closes the ledger. Escape, Close and the Mod Bar button also dismiss it.

## How the Accounting Works

Rates reflect actual production and financial activity over the selected observation window. Session history is retained for up to 24 hours; activity before the session began is unavailable. Longer windows can make intermittent dungeon rewards easier to assess. The status shows when a full window is still being collected.

- Income and operating expenses are attributed when transactions occur.
- Container collection tray changes are combined with wallet credits so income is recognized when earned without double-counting.
- Pure routing nodes and storage pass-throughs are excluded to ensure true net production measurements.
- In and Out show observed production/earnings and consumption/spending, not portal transfers or estimated demand.
- Coin display uses the largest two denominations. For example, 1 gold, 3 silver and 98 copper displays as 1 gold and 4 silver; recorded accounting totals remain exact.

## Requirements

- Kingdom Inc (Windows x64 Mono)
- [BepInEx 5](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.6) (5.4.23.6 or newer)
- [Foundation](https://www.nexusmods.com/kingdominc/mods/18) (0.14.2 or newer)

## Installation

1. Install BepInEx 5 for Kingdom Inc.
2. Install Foundation (0.14.2 or newer).
3. Extract the release ZIP into your Kingdom Inc game directory.
4. Verify the file exists at `BepInEx/plugins/GlobalLedger/GlobalLedger.dll`.
5. Launch the game and access Global Ledger from the top Mod Bar!

## Compatibility

| Class.Method | Hook Type | Impact |
|---|---|---|
| FlowIO.ConsumeItem | Foundation hook (postfix) | Observes item consumption events across production chains. |
| GamePlayer.AddCurrency | Foundation hook (postfix) | Captures currency income credits from shops, quests, and rewards. |
| GamePlayer.SpendCurrency | Foundation hook (postfix) | Tracks currency spending events for production operating costs. |
| WarProgress.UpdateWar | Foundation execution observer | Observes war effort currency income and progress updates. |
| GameUI.ProcessEscape | Foundation hook (prefix) | Closes the Global Ledger dropdown panel when pressing Escape. |
| GameUI.ProcessCancel | Foundation hook (prefix) | Closes the ledger on a short right-click when Right Click Fix is active; preserves dragging, native right-click actions and modal menus. |
| GameUI.ToggleUI | Foundation hook (postfix) | Synchronizes Global Ledger dropdown visibility with native HUD toggle states. |
| FlowElement lifecycle methods | Foundation execution observer | Dynamically tracks production scopes across building updates and craft operations. |
| FoundationApi.Events.Subscribe | Foundation event subscription | Refreshes workspace, container and recipe grouping after relevant changes. |
| FoundationApi.ExecutionHooks.Subscribe | Foundation execution observer (WorkspaceEdit) | Refreshes grouping after workspace edits. |
| FoundationApi.GameHooks.Subscribe | Foundation hooks (DecorationStyleUpdate, BuildingClearPorts) | Refreshes cached locations and building information after edits. |

## Removal

Close the game and delete `BepInEx/plugins/GlobalLedger/`. Accounting history is session-local and leaves no persistent custom data in your saves.

## Credits and Source

Created by **Urakka** (rockyamyx). [Source code on GitHub](https://github.com/KingdomIncModding/GlobalLedger). Game artwork belongs to its respective owners.
