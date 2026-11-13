# Global Ledger

**Current release: 0.1.12 — for Kingdom Inc game version 1.0.14.**

[GitHub releases](https://github.com/KingdomIncModding/KingdomInc_GlobalLedger/releases) · [Nexus Mods](https://www.nexusmods.com/kingdominc/mods/19) · Version 0.1.12

Track live currency earnings, operating expenses, and item production across your entire kingdom from a dropdown on the top Mod Bar in Kingdom Inc.

Monitor economy flow in real time without placing physical tracking nodes in every factory line.

## Features

- **Top Mod Bar Dropdown:** Access real-time financial and production data from the top Mod Bar at any time.
- **Basic and Advanced Modes:**
  - **Basic Mode:** Clean, scrollable summary of Currency Net Collection and Item Net Production sorted by Tier -> A–Z.
  - **Advanced Mode:** Full hierarchical drill-down by Currency / Item > Container > Building > Operating Costs.
- **Rate Scaling (sec / min / hour):** Switch rate calculations dynamically between per-second, per-minute, or per-hour display.
- **Configurable Rolling Averages:** Smooth out rate spikes via a 2–30 sample rolling average, toggleable directly from the UI header or Mod Config.
- **Multi-Currency Accounting:** Gold, Silver, and Copper use native denominations; Requisition, Mana, Crowns, Medals, and Rings maintain their own units.
- **Container & Workspace Grouping:** Automatically groups production by blueprint containers and workspaces, with container icon stickers and clean names.
- **Live View Navigation:** Click the View button on any container or building row to switch workspaces, pan the camera, and highlight the building.

## How the Accounting Works

Rates reflect actual production and financial activity sampled in one-second intervals over a rolling 60-second window.

- Income and operating expenses are attributed when transactions occur.
- Container collection tray changes are combined with wallet credits so income is recognized when earned without double-counting.
- Pure routing nodes and storage pass-throughs are excluded to ensure true net production measurements.

## Requirements

- Kingdom Inc (Windows x64 Mono)
- [BepInEx 5](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.6) (5.4.23.6 or newer)
- [Foundation](https://www.nexusmods.com/kingdominc/mods/18) (0.14.0 or newer)

## Installation

1. Install BepInEx 5 for Kingdom Inc.
2. Install Foundation (0.14.0 or newer).
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
| GameUI.ToggleUI | Foundation hook (postfix) | Synchronizes Global Ledger dropdown visibility with native HUD toggle states. |
| FlowElement lifecycle methods | Foundation execution observer | Dynamically tracks production scopes across building updates and craft operations. |

## Removal

Close the game and delete `BepInEx/plugins/GlobalLedger/`. Accounting history is session-local and leaves no persistent custom data in your saves.

## Credits and Source

Created by **Urakka** (rockyamyx). [Source code on GitHub](https://github.com/KingdomIncModding/KingdomInc_GlobalLedger). Game artwork belongs to its respective owners.
