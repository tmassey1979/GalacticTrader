# Unity Dashboard Parity Checklist

Story: `#267` Unity Migration Dashboard module

- [x] Summary metrics are sourced from the same backend API surfaces as WPF.
- [x] Action-first board model implemented (`DashboardActionBoard` + `DashboardActionPlanner`).
- [x] Event feed filtering implemented (`DashboardEventFeedFilter`).
- [x] Event feed CSV export implemented (`DashboardEventFeedCsvExporter`).
- [x] Realtime strategic snapshots project into dashboard state (`DashboardRealtimeStateProjector`).
- [x] Unity dashboard module consumes shared dashboard service and realtime projector (`UnityDashboardModuleController`).
- [x] Module-level parity tests added and passing:
  - `DashboardModuleServiceTests`
  - `DashboardEventFeedUtilitiesTests`
  - `DashboardRealtimeStateProjectorTests`
