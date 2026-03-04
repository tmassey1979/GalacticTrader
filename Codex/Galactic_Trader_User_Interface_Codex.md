# GALACTIC TRADER
## User Interface Codex
### Version 2.1 (Unity Migration Aligned)

Date: 2026-03-04

## 1. UI Philosophy

- Action-first over dashboard-first.
- Online-only gameplay (no local/offline mode).
- Fast command-to-feedback loops for trading, navigation, combat, and strategic actions.
- Progressive disclosure: core actions always visible, deeper analytics optional.

## 2. Primary UI Platform

- Legacy client: WPF (`src/Desktop`) for transitional parity and operations.
- Primary target client: Unity (`unity/`).
- Shared behavior layer: `src/Shared/GalacticTrader.ClientSdk.csproj`.

## 3. Shell and Navigation Model

- Unity shell pattern uses module lifecycle controllers instead of WPF tab-hosting.
- Standardized module UX states:
  - `Loading`
  - `Ready`
  - `Offline`
  - `Error`
- Hotkey routing and module switching are driven by shared shell primitives.

## 4. Current Unity Module Status

- Implemented migration slices:
  - Auth/session lifecycle (`UnityAuthController`)
  - Shell/module host (`UnityModuleHostController`, `UnityShellModule`)
  - Realtime stream controller (`UnityRealtimeController`)
  - Dashboard module (`UnityDashboardModuleController`)
  - Starmap streaming subsystem (`UnityStarmapStreamingController`)
  - Trading module (`UnityTradingModuleController`)
- In progress:
  - Routes and navigation planning module migration
- Planned:
  - Fleet/Battles
  - Intel/Reputation/Territory
  - Settings/hotkeys migration polish
  - Final visual system pass and QA cutover

## 5. Starmap UX and Performance

- Full-world materialization is explicitly avoided.
- Shared starmap planner provides:
  - chunked active-window planning
  - distance culling
  - frustum culling
  - LOD tiers
- Unity client renders from planned frame slices, not from eager full-map scene build.
- Performance budgets and benchmark evidence are tracked in `docs/unity-starmap-performance-budgets.md`.

## 6. Trading UX Baseline

- Trading UX supports:
  - market listing load
  - transaction history
  - economy price preview
  - trade execution
- Spread and fee insights are produced by shared trading models:
  - listing-level spread/fee summaries
  - preview-level spread/fee estimation
- Error handling is user-friendly via mapped failure states (credits/cargo/quantity/rate-limit/auth).
- Parity checklist: `docs/unity-trading-parity-checklist.md`.

## 7. Dashboard UX Baseline

- Dashboard is a command board, not just a status wall.
- Shared dashboard planner prioritizes next actions across trading, routes, fleet, and intel signals.
- Realtime strategic snapshot projection updates board/feed state continuously.
- Parity checklist: `docs/unity-dashboard-parity-checklist.md`.

## 8. Routes UX Direction

- Route UX preserves:
  - waypoint parsing
  - mode presets
  - plan optimization profiles
  - risk simulation projection
  - starmap route overlays
- Shared routes module service/controller migration is the active implementation slice.

## 9. Visual Language Direction

- Target visual direction: game-like tactical command HUD rather than enterprise dashboard chrome.
- Keep action controls primary and information panes secondary.
- Maintain readability with clear severity coding for risk, legal exposure, and combat pressure.

## 10. UI Technical Architecture

- Unity module controllers orchestrate UI workflows.
- Shared SDK services provide deterministic state derivation and parity-safe behavior.
- Backend remains authoritative for simulation and persistence.
- Realtime channels synchronize strategic and communication updates.

## 11. Codex Update Contract

When UI architecture changes, update together:

- `Codex/Galactic_Trader_User_Interface_Codex.md` (this file)
- `docs/unity-client-architecture.md`
- Unity module parity docs under `docs/unity-*-parity-checklist.md`
- `unity/README.md`
