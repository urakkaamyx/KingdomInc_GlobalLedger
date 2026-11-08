Global Ledger 0.1.10 for Kingdom Inc

Track live currency earnings, operating expenses, and item production across
your entire kingdom from the top Mod Bar.

INSTALL / UPDATE
Install the requirements listed on the Nexus page first:
https://www.nexusmods.com/kingdominc/mods/19
Close Kingdom Inc and extract this ZIP into the KingdomInc game folder.
Installed file: BepInEx/plugins/GlobalLedger/GlobalLedger.dll
Restart the game after installation or an update.

FEATURES & USE
- Rolling average toggle on UI: Click the Rolling Avg button in the top-right
  header bar to instantly toggle rolling averages on and off without leaving
  the ledger. The button highlights Gold when active.
- Mod Config menu settings: Access the Global Ledger section in the Mod Config
  menu to configure rolling averages:
  - Rolling average checkbox: Toggle rolling averages on or off to make rate
    calculations less jumpy.
  - Entry Count slider: Choose how many entries to average (default: 3).
    When selecting sec, rates are averaged across the chosen number of seconds.
    When selecting min or hour, rates are averaged across the chosen number of
    minutes.
- Click Global Ledger on the right of the top Mod Bar to open or close the
  dropdown. While open, you can freely pan (WASD and mouse drag), zoom, and
  click on the workspace without closing the ledger.
- Basic | Adv modes: Toggle between Basic summary and Adv hierarchical view
  using the buttons in the top-right corner.
  - Basic mode: Streamlined summary listing Currency Net Collection and Item
    Net Production ([Icon] [Name] +/-XXX[icon] / {sec|min|hour}), with items
    sorted by Tier -> A-Z. Currencies and items with no production in the recent
    window are cleanly hidden from Basic mode unless Show All is clicked.
  - Adv mode: Full breakdown (Currency / Item > Container > Building >
    Operating Costs). Defaults to everything collapsed for clean drilling down.
- Rate time unit toggle: Click [sec | min | hour] in the header bar (defaults
  to min) to scale all rates to per-second, per-minute, or per-hour.
- Currencies tab (Adv): View every unlocked currency's rolling net rate over the
  last 60 seconds (Gold is written out in full Gold, Silver, and Copper
  denominations with native coin symbols; other currencies use their native
  units). Expand a currency to inspect containers, individual buildings, and
  Operating Costs (icon-only currency rows).
- Production tab (Adv): View unlocked items sorted by Tier -> A-Z and their rolling
  net production rate (Produced - Consumed). Items with zero activity are
  hidden by default; click Show All in the header bar to show or hide
  zero-activity unlocked items. Expand an item to inspect containers,
  buildings, and Operating Costs (consumed input items and spent currencies as
  icon-only rows).
- Container indicator: Container rows display [ContainerIcon] | [ProductionIcon]
  [ContainerName] using the native green container icon, a | separator, and
  the container's overlapping icon sticker (or its building's production icon),
  with TextMeshPro rich-text tags stripped from container and workspace names.
- Collapse All & View: Click a row or its left arrow to expand or collapse
  without moving the camera. Click Collapse All in the header bar to collapse
  every open group on the active tab. Hover a container or building row to
  highlight it in the current workspace, or click its right-aligned View
  button to switch workspaces, center and highlight the target for five
  seconds without closing the dropdown.

SAVES / REMOVAL
Accounting history is session-local and adds no custom buildings, research,
or save data. Close the game and delete BepInEx/plugins/GlobalLedger to
remove the mod.

By rockyamyx / Urakka. Native game frames, fonts, and icons are used from
the installed game and are not redistributed.
