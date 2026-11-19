# Global Ledger V2 - Cleaner, Crisper HD

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/S6V62AX30M)

**The cleaner, crisper HD edition of Global Ledger for Kingdom Inc 1.0.14.**

The original Global Ledger remains available as the main download on the [same Nexus page](https://www.nexusmods.com/kingdominc/mods/19). Choose **Global Ledger V2 - Cleaner Crisper HD** under Optional Files for this redesigned edition. Installing HD uses a separate plugin folder and does not overwrite the original.

## A cleaner ledger

- Sharp native frames, readable text, consistent spacing and aligned **In / Out / Net** columns.
- **Basic** provides a currency and production overview. **Advanced** drills into workspaces, containers, buildings and operating costs.
- A compact window with smaller Sort and Display controls.
- Click the gold-framed **KINGDOM OVERVIEW** button to hide or show the control sections. The window shrinks to fit; your settings stay selected.
- Click the red **Global Ledger** ModBar button to show or hide the entire ledger. Close, Escape, and Right-click also dismiss it.
- Search assets and sources, sort the results, show unlocked assets, or collapse expanded rows.
- Use View buttons to find containers and buildings in your kingdom.
- Hover over a shortened name for two seconds to scroll it horizontally. The text pauses at the end, returns smoothly, and resets when you move away; the amount columns stay fixed.

## Rates and money

- Seconds: **1s / 10s / 30s**.
- Minutes: **1m / 5m / 10m**.
- Hours: **1h / 12h / 24h**.
- Labels and calculations follow the selected unit and observation window, showing partial-window collection progress until the full window has elapsed.
- Enable rolling calculation for a smoother display; choose the sample count in Mod Config.
- Set the display refresh interval from 1 to 10 seconds, or freeze the displayed snapshot while activity continues to be recorded.
- Money uses native coin icons and shows the largest two denominations, using overflow-safe decimal totals while rounding copper into silver when gold is present (for example, **1 gold, 3 silver, 98 copper** displays as **1 gold and 4 silver**). Recorded totals remain exact.
- History is kept for the current session up to 24 hours via memory-bounded tiered buckets. A newly started session has no earlier activity to display.

## Requirements

- Kingdom Inc **1.0.14**, Windows Mono.
- [BepInEx 5](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.6), version 5.4.23.6 or newer.
- [Foundation](https://www.nexusmods.com/kingdominc/mods/18), version **0.14.4 or newer**. Install the Foundation runtime download.

## Installation

1. Close Kingdom Inc.
2. Install BepInEx 5 and Foundation.
3. Extract the HD release ZIP into your Kingdom Inc game folder.
4. Launch the game and click **Global Ledger** on Foundation's ModBar.

The HD plugin installs to `BepInEx/plugins/GlobalLedgerV2/GlobalLedgerV2.dll`. BepInEx and Foundation are separate downloads.

**Use one edition at a time.** If both editions are installed, HD initializes first and then unloads the original Global Ledger for that game session, including its button and accounting hooks. Only HD remains active. The original DLL, settings and Nexus downloads are preserved. Remove HD and restart the game to use the original again.

## Compatibility

| Class.Method | Hook Type | Impact |
|---|---|---|
| FlowIO.ConsumeItem | Foundation hook (postfix) | Records consumed items in operating costs. |
| GamePlayer.AddCurrency | Foundation hook (postfix) | Attributes currency income to the active building. |
| GamePlayer.SpendCurrency | Foundation hook (postfix) | Records successful currency spending. |
| WarProgress.UpdateWar | Foundation execution observer | Attributes war earnings to war-effort sources. |
| GameUI.ProcessCancel | Foundation hook (prefix, priority 100) | Dismisses the ledger on right-click before cancel overrides. |
| GameUI.ProcessEscape | Foundation hook (prefix) | Dismisses the ledger with Escape. |
| GameUI.ToggleUI | Foundation hook (postfix) | Closes the ledger when the HUD is hidden. |
| FlowElement & FrameworkWorkspaceNode lifecycle | Foundation execution observer & node Update | Observes native and custom-hosted building production, currency, and crafting activity. |
| DevSpawner icon updates | Foundation hook (GameHookPoints.SpawnerIcon) | Updates spawner building icons immediately when recipes change. |
| FoundationApi.Events.Subscribe | Foundation event subscription | Updates workspace and container grouping after relevant changes. |
| FoundationApi.ExecutionHooks.Subscribe | Foundation execution observer (WorkspaceEdit) | Refreshes grouping after workspace edits. |
| FoundationApi.GameHooks.Subscribe | Foundation hooks (DecorationStyleUpdate, BuildingClearPorts) | Refreshes location and building information after edits. |

Game integration is provided by Foundation. HD observes activity and changes its own ledger UI; it does not change production recipes or write custom ledger data into saves.

## Removal

Close the game and remove `BepInEx/plugins/GlobalLedgerV2/`. If the original Global Ledger is still installed, it becomes active again on the next launch.

## Credits

Created by **Urakka** (rockyamyx). Native game artwork belongs to its respective owners.

[HD source code](https://github.com/KingdomIncModding/GlobalLedgerV2) | [Original Global Ledger](https://github.com/KingdomIncModding/GlobalLedger)

## Support

If you enjoy these Kingdom Inc mods and tools, consider supporting development on Ko-fi!

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/S6V62AX30M)
