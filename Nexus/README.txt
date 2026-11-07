Global Ledger 0.1.7 for Kingdom Inc

Track live currency earnings, operating expenses, and item production across
your entire kingdom from the top Mod Bar.

INSTALL / UPDATE
Install the requirements listed on the Nexus page first:
https://www.nexusmods.com/kingdominc/mods/19
Close Kingdom Inc and extract this ZIP into the KingdomInc game folder.
Installed file: BepInEx/plugins/GlobalLedger/GlobalLedger.dll
Restart the game after installation or an update.

FEATURES & USE
- Click Global Ledger on the right of the top Mod Bar to open or close the
  dropdown. While open, you can freely pan (WASD and mouse drag), zoom, and
  click on the workspace without closing the ledger.
- Basic | Adv modes: Toggle between Basic summary and Adv hierarchical view
  using the buttons in the top-right corner.
  - Basic mode: Streamlined summary listing Currency Net Collection and Item
    Net Production ([Icon] [Name] +/-XXX[icon] / {sec|min|hour}), with items
    sorted by Tier -> A-Z.
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
