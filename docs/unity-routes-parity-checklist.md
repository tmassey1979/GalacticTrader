# Unity Routes Parity Checklist

Story: `#269` Unity Migration: migrate Routes and navigation planning module

## Scope

- Route planning queries and optimization profiles in Unity.
- Waypoint parsing and multi-leg route composition.
- Risk simulation projection for route plans.
- Starmap overlay projection for planned, dangerous, and suggested routes.

## Parity Checks

- [x] Shared routes module service exists in `GalacticTrader.ClientSdk.Routes`.
- [x] Route planning supports travel-mode presets and algorithm selection.
- [x] Waypoint input parsing resolves by sector name or GUID token.
- [x] Multi-leg waypoint plans merge into a single route plan for UI.
- [x] Risk simulation projection includes projected risk band and protection estimate.
- [x] Overlay projection emits planned + dangerous + suggested route edges.
- [x] Unity module controller scaffold can refresh, plan, and load optimizations.
- [x] Regression tests cover state load, waypoint parsing, planning, and optimization.

## Remaining UI Work

- Unity scene/prefab implementation for route planner input controls and optimization cards.
- Visual styling pass for starmap route overlays and risk simulation panel UX.
- Final gameplay polish for action-first route workflows.
