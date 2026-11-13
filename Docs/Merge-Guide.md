# Global Ledger V2 â€” future merge guide

Global Ledger V2 is released separately as the Cleaner, Crisper HD edition for game 1.0.14, based on Global Ledger 0.1.12. Its repository is https://github.com/KingdomIncModding/GlobalLedgerV2. It shares the existing Nexus page as a distinct optional download. The original repository, plugin and downloads must be preserved; no merge or replacement is part of this release.

The production project references the existing Foundation runtime, BepInEx and Unity. It has no game-assembly or Harmony reference. All game interception uses existing Foundation APIs.

| Area | Global Ledger V2 source | Merge responsibility |
| --- | --- | --- |
| Observation policy | `src/Accounting/GlobalLedgerObservationPlan.cs` | Skip known transport/storage snapshots; retain conservative handling of unknown types and live recipe choices. |
| Operation accounting | `src/Accounting/GlobalLedgerObserver.cs`, `GlobalLedgerFrame.cs`, `GlobalLedgerBook.cs` | Preserve till-plus-wallet accounting and visitor rules; commit one pooled batch per operation. |
| Building descriptions | `src/Accounting/GlobalLedgerSourceCache.cs` | Refresh on recipe, label, workspace or position changes; reuse unchanged descriptions. |
| Locations | `src/World/GlobalLedgerLocations.cs` | Keep cached placements, existing-event invalidation and periodic reconciliation for direct edits. |
| War attribution and navigation | `src/Accounting/Patches/GlobalLedgerWarProgressPatch.cs`, `src/World/GlobalLedgerTarget.cs` | Reuse cached workspace discovery. |
| Lifecycle | `src/Core/GlobalLedgerV2Plugin.cs` | Own one shared location index, dispose subscriptions and reset state on kingdom changes. |

Global Ledger V2 uses the namespace `KingdomGlobalLedgerV2` and explicit `GlobalLedger` class prefixes. Adapt those names to Global Ledger's existing internals during a future merge. Preserve Global Ledger's plugin identity, settings and Mod Bar entry when merging; the separately released HD identity must remain independent unless a future merge is explicitly authorized.

Verification-only source is in `src/Verification/` and compiled exclusively by `GlobalLedgerV2.Verification.csproj`. Its baseline adapter deliberately refers to the original `KingdomGlobalLedger.Ledger*` internal type names so it can compare the two actual assemblies. Those references identify Global Ledger's source internals, not the separate Ledger mod.

Tests cover native production and crafting callbacks, consumption, currency and till accounting, returning visitors, nested scopes, rolling windows, cache invalidation and location grouping parity. Synthetic timing results do not establish whole-game FPS or compatibility with every mod-owned building.
