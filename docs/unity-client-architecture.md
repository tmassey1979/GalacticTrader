# Unity Client Architecture Baseline

This document defines the initial architecture baseline for migrating the desktop client from WPF to Unity.

## Objectives

- Keep gameplay online-only with backend APIs as source of truth.
- Shift UX from status-heavy dashboarding to action-first gameplay loops.
- Avoid full-world startup rendering and support scalable map streaming.

## Client Layers

- `Presentation`: Unity scenes, HUD, diegetic UI panels, input maps.
- `Application`: use-case handlers for player actions (trade, route, combat, fleet, strategic).
- `Infrastructure`: API gateway client, auth/session handling, realtime stream clients, storage.
- `Domain View Models`: immutable projections for UI state and action feedback.

## Module Map

- `Auth`: login/logout/session lifecycle.
- `Shell`: scene navigation, module host, top-level HUD.
- `Starmap`: streaming/LOD/culling renderer and interaction surface.
- `Trading`: listings, preview, execute, history.
- `Routes`: planning, optimization, risk simulation.
- `Fleet`: ships, modules, crew, escort.
- `Battles`: combat logs and battle actions.
- `Intel`: reports and threat alerts.
- `Reputation`: standings and benefits.
- `Territory`: dominance and policy actions.
- `Communication`: channels and realtime events.

## Shared Client SDK

- Shared API contracts and client implementations now live in `src/Shared/GalacticTrader.ClientSdk.csproj`.
- Desktop references this SDK directly, and Unity can consume the same assembly to keep API parity.
- Existing DTO/client namespaces are preserved for compatibility during migration.
- HTTP error and JSON handling are standardized through shared runtime helpers:
  - `ApiClientException` for non-success responses
  - `ApiClientRuntime` for consistent bearer token, status handling, and JSON reads
- Session lifecycle is encapsulated in `AuthSessionManager` with:
  - persisted-session restore hooks
  - JWT expiry detection
  - optional refresh delegate for token renewal
  - deterministic logout/clear behavior
  - explicit `AuthFailureState` values for UI messaging
- Shell navigation lifecycle primitives are encapsulated in `GalacticTrader.ClientSdk.Shell`:
  - `ModuleHostCoordinator` for module switching and cleanup lifecycle
  - `ModuleHotkeyRouter` for module hotkey resolution
  - standardized module UX state snapshots (`Loading`, `Ready`, `Offline`, `Error`)
- Dashboard action planning primitives are encapsulated in `GalacticTrader.ClientSdk.Dashboard`:
  - `DashboardModuleService` to aggregate gameplay-relevant dashboard signals
  - `DashboardActionPlanner` to convert signals into prioritized next actions
  - `DashboardActionBoard` as a UI-ready action-first model
  - `DashboardEventFeedFilter` and `DashboardEventFeedCsvExporter` for feed workflows
  - `DashboardRealtimeStateProjector` for strategic realtime snapshot updates
- Realtime coordination primitives are encapsulated in `GalacticTrader.ClientSdk.Realtime`:
  - `RealtimeCoordinator` for strategic + communication stream lifecycle
  - duplicate-subscription protection and safe start/stop semantics
  - diagnostics snapshots for message/fault observability
- Trading module primitives are encapsulated in `GalacticTrader.ClientSdk.Trading`:
  - `TradingModuleService` for listings/history load, preview, and trade execution orchestration
  - `TradingListingSummary` and `TradingPreviewSummary` for spread/fee visualization models
  - `TradingOperationResult`/`TradingOperationFailureState` for user-friendly failure mapping

## Action-First UX Principles

- Primary screen focus is on available actions, not telemetry blocks.
- Every module surfaces:
  - direct action buttons
  - immediate precondition validation
  - clear outcome feedback and rollback-friendly messaging
- Secondary analytics live behind expand/collapse or dedicated subviews.

## Runtime Performance Baseline

- No full-map 3D materialization at startup.
- Scene loading must be progressive with explicit budget controls.
- Realtime subscriptions are scoped by active module and cleaned up on module exit.

## Starmap Streaming Baseline

- Shared streaming primitives are defined in `GalacticTrader.ClientSdk.Starmap`:
  - chunk indexing (`StarmapChunkIndex`, `StarmapChunkingOptions`)
  - frame planning (`StarmapStreamingPlanner`, `StarmapFramePlan`)
  - render budgets (`StarmapRenderBudget`)
  - distance-based LOD tiers (`StarmapLodBands`, `StarmapLodTier`)
- Planner applies both distance culling and camera-forward frustum culling via `StarmapCameraState`.
- Unity should render from planned frame slices (`UnityStarmapStreamingController`) instead of eagerly constructing the full world.
- Route rendering is constrained to visible/rendered sector sets to avoid hidden-route overdraw.
- Performance budgets are tracked in `docs/unity-starmap-performance-budgets.md`.

## Trading Module Baseline

- Trading module state must expose:
  - market listings
  - transaction history
  - derived listing spread/fee summaries
  - last known player liquidity snapshot
- Trade preview uses economy simulation output plus observed fee rates from recent transactions.
- Trade execution maps backend failures to action-friendly UI states (credits/cargo/quantity/rate-limit/auth).
- Unity controller scaffold (`UnityTradingModuleController`) loads state, previews trades, and executes buy/sell actions via shared `TradingModuleService`.
- Parity checklist is tracked in `docs/unity-trading-parity-checklist.md`.

## Platform Targets

- Primary release targets: `Windows`, `Linux`, `macOS`.
- Planned console path: `Xbox` and `PS4` after platform approvals and restricted SDK onboarding.
- Shared `ClientSdk` logic avoids platform-specific dependencies so gameplay/auth/shell/starmap behavior remains portable across desktop and console clients.

## Delivery Slices

- Slice 1: Architecture, shared SDK contracts, auth/session, shell.
- Slice 2: Action-critical modules (starmap/trading/routes/fleet).
- Slice 3: Strategic/realtime/polish and packaging/cutover.
