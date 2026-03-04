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

## Delivery Slices

- Slice 1: Architecture, shared SDK contracts, auth/session, shell.
- Slice 2: Action-critical modules (starmap/trading/routes/fleet).
- Slice 3: Strategic/realtime/polish and packaging/cutover.
