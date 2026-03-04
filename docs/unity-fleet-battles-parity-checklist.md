# Unity Fleet and Battles Parity Checklist

Story: `#270` Unity Migration: migrate Fleet and Battles modules

## Scope

- Fleet management views and core fleet actions in Unity.
- Battles log visibility and outcome-focused combat summaries.
- Stable refresh and realtime snapshot projection behaviors.

## Parity Checks

- [x] Shared fleet module service exists in `GalacticTrader.ClientSdk.Fleet`.
- [x] Fleet state exposes ship templates, ship list, escort summary, and derived fleet summary.
- [x] Fleet actions include ship purchase and convoy simulation workflows.
- [x] Fleet realtime projection updates fleet strength/protection from strategic snapshots.
- [x] Shared battles module service exists in `GalacticTrader.ClientSdk.Battles`.
- [x] Battles state exposes recent combat logs, active combats, and outcome summary metrics.
- [x] Battles actions include start/tick/end combat workflows through shared API client methods.
- [x] Battles realtime projection ingests combat events from strategic snapshots and refreshes outcome summary.
- [x] Unity module controller scaffolds exist for both fleet and battles modules.
- [x] Regression tests cover fleet summary/projection and battles summary/projection/service actions.

## Remaining UI Work

- Unity scene/prefab implementation for fleet management controls and combat timeline visuals.
- Final visual polish and interaction tuning against action-first UX quality bar (`#277`).
