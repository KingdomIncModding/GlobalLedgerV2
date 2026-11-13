# Global Ledger V2 — local testing

Global Ledger V2 is the separate Cleaner, Crisper HD edition for Kingdom Inc 1.0.14. It uses the existing Foundation 0.14.2 runtime and has its own **Global Ledger V2** Mod Bar button and settings. Its plugin identifier is `urakka.kingdominc.globalledgerv2`, and its assembly is `GlobalLedgerV2.dll`.

The existing **Global Ledger** and the separate **Ledger** mod keep their own identities. Global Ledger V2 does not replace either mod. When both editions are installed, HD unloads the original component at startup. Its OnDestroy releases its hooks and Foundation UI registration. The original files and configuration remain intact.

## Build and install locally

From the GlobalLedgerV2 folder:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools\Build-Local.ps1
```

This builds and creates a local install-ready ZIP under `Release/`. To install the separate Global Ledger V2 DLL and metadata as well, close the game and add `-Install` to that command. The package contains only `BepInEx/plugins/GlobalLedgerV2/GlobalLedgerV2.dll` and its `mod.json`.

Click the red **Global Ledger** button on Foundation's Mod Bar to show or hide Global Ledger V2. The ledger opens directly beneath that button; the red tab belongs to the Mod Bar, and is not duplicated inside the popup. The bottom Close button and Escape also dismiss the ledger. Configuration is saved separately in `BepInEx/config/urakka.kingdominc.globalledgerv2.cfg`. Activity history is session-only.

For an independent performance comparison, run one Global Ledger version at a time. The separate Ledger mod is unrelated to that comparison.

## Implementation behavior

Global Ledger V2 subscribes to existing Foundation activity, currency, consumed-input and workspace notifications. It keeps the original before/after accounting rules, batches each observed operation into one bucket update, skips snapshots for known transport/storage nodes, and caches building descriptions and locations.

Location entries are rebuilt after relevant workspace edits or when a previously unseen accounting source appears. Direct model changes that bypass Foundation notifications are reconciled within two seconds while accounting or the display is active. The display refresh interval is selectable from one to ten seconds. An item moving between buffers is not counted repeatedly as newly produced.

Foundation's execution dispatch remains unchanged. Global Ledger V2 does not introduce new Foundation API members or dedicated native craft-completion events.

## Isolated verification

Build `GlobalLedgerV2.csproj`, stage its DLL in `Release/`, then build the verification project:

```powershell
dotnet build GlobalLedgerV2.Verification.csproj -c Release -p:MSBuildProjectExtensionsPath=_AI\Scratch\Verification\obj\
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools\Verification\Run-Verification.ps1 -RunName comparison
```

The runner creates an isolated game process with Global Ledger 0.1.12, Global Ledger V2 and the unchanged Foundation runtime. It first verifies that HD unloads the original and leaves only its own ModBar registration. For accounting parity only, the verification plugin temporarily recreates the original component and disposes it before UI checks. Add `-WithoutOriginal` to verify standalone HD startup and native accounting callbacks. Visual verification also covers delayed name scrolling, clipping, refresh continuity and pooled-row resets. The verification plugin uses synthetic fixtures, blocks normal saves and save loading, writes its results inside the isolated runtime, and exits. Each run requires a new name. The verification plugin is excluded from the player build and local installation package.

The isolated runtime contains junctions to the game data and Mono directories. Keep those targets intact if removing a runtime manually.

## Designer layout

The Basic and Advanced layouts follow `05-Global-Ledger-v2.zip`: an 816 × 856 logical panel, 24-pixel outer insets, four header control groups, aligned In / Out / Net columns, 28-pixel section headings and 48-pixel data rows. The panel scales uniformly when the space beneath the Mod Bar is smaller. Foundation owns the Mod Bar's 40-pixel-high slot; its Global Ledger button uses the design's red tab artwork within that slot.

The three radios select the accounting window for the chosen unit: 1s / 10s / 30s, 1m / 5m / 10m, or 1h / 12h / 24h. Changing units preserves the selected radio position and updates both its suffix and duration. Session activity is retained for up to 24 hours; short windows use a time index to skip older buckets. The rate stepper selects per second, minute or hour, normalized to that window. Rolling calculation smooths the displayed totals over the configured number of refresh samples. Sort applies within each section or hierarchy level. Search includes matching sources under collapsed parents. Refresh ranges from 1 to 10 seconds. Freeze holds the accounting snapshot while the observer continues recording activity. Show all lists unlocked assets without requiring recent activity. Advanced retains expansion and location navigation.

For an isolated render and interaction check, build both projects as above, then run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools\Verification\Run-Verification.ps1 -RunName designer-check -Visual
```

This writes Basic and Advanced 1920 × 1080 screenshots and a 1280 × 720 screenshot alongside the accounting and UI verification results. Fixtures use synthetic data and do not load or save the player's kingdom. Foundation 0.14.2 or newer must be installed to use this mod.

Money cells show at most two denominations with coin icons. When gold is present, copper rounds to the nearest silver (half a silver rounds up), carrying into gold when needed: 1 gold, 3 silver, 98 copper displays as 1 gold and 4 silver. Below one gold, silver and copper remain visible; copper-only amounts show just copper. Negative values use the same rounding. Recorded accounting totals remain exact.

Compact layout: the window is 816 pixels wide (previously 960). Refresh is 52 pixels wide and both Sort selectors are 92 pixels wide, half their original widths. Display is 192 pixels wide and Sort is 124. Table rows and headings share a 736-pixel content area, with aligned money columns and location buttons.

Click the KINGDOM OVERVIEW heading to hide or show the Rate, Mode, Sort and Display sections. The ledger and footer move up and the window becomes 120 pixels shorter while those controls are hidden. The heading, subtitle and live status remain visible, and selected settings are preserved. The same heading button works in Advanced mode when its caption changes to CURRENCY BREAKDOWN or PRODUCTION BREAKDOWN.

The HD edition has its own GitHub repository at https://github.com/KingdomIncModding/GlobalLedgerV2. Its separate Optional Files download shares https://www.nexusmods.com/kingdominc/mods/19 with the preserved original. The initial publication creates a separate optional file group; subsequent releases use the configured HD file group. Preserve the original primary download, page version and original page content in both cases.
