# Strategic Systems Phase-1 Migration Plan

This document defines the rollout plan for Phase-1 strategic systems:
- Sector volatility cycles
- Corporate wars
- Infrastructure ownership
- Territory dominance tracking

## Scope
New persistence models:
- `SectorVolatilityCycle`
- `CorporateWar`
- `InfrastructureOwnership`
- `TerritoryDominance`

New APIs:
- `GET/POST /api/strategic/volatility`
- `GET/POST /api/strategic/corporate-wars`
- `GET/POST /api/strategic/infrastructure`
- `GET /api/strategic/territory-dominance`
- `POST /api/strategic/territory-dominance/recalculate/{factionId}`

## Rollout Steps
1. Ship schema objects in the application model (completed in this phase).
2. Validate behavior in test and dev environments using in-memory and PostgreSQL-backed API runs.
3. Promote to staging with backup guardrails enabled (`nightly-backups.yml`).
4. Run baseline strategic recalculation calls for major factions after deployment.
5. Enable monitoring dashboards/alerts for strategic endpoint latency and error rates.

## EF Migration Execution (Production)
When moving from `EnsureCreated` bootstrap to managed migrations:

1. Generate migration:
   - `dotnet ef migrations add StrategicSystemsPhase1 --project src/Data --startup-project src/API`
2. Review generated SQL:
   - Ensure indexes and FK constraints match strategic entity configuration.
3. Apply in staging:
   - `dotnet ef database update --project src/Data --startup-project src/API`
4. Apply in production during low-traffic window with DB backup taken immediately prior.

## Backward Compatibility
- APIs are additive; no existing endpoint contracts are removed.
- Strategic tables do not modify existing columns.
- FKs are linked to existing `Sectors` and `Factions` records only.

## Recovery
- If deployment rollback is required:
  - Restore most recent PostgreSQL backup (`backups/postgres/*.sql.gz`).
  - Re-run resilience verification scripts from `docs/resilience-runbook.md`.
