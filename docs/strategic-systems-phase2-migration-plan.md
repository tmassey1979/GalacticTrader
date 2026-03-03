# Strategic Systems Phase-2 Migration Plan

Phase-2 extends long-term strategic systems with:
- Insurance economy
- Intelligence networks

## New Persistence Models
- `InsurancePolicy`
- `InsuranceClaim`
- `IntelligenceNetwork`
- `IntelligenceReport`

## New API Surface
- `GET/POST /api/strategic/insurance/policies`
- `GET/POST /api/strategic/insurance/claims`
- `POST /api/strategic/intelligence/networks`
- `GET/POST /api/strategic/intelligence/reports`
- `POST /api/strategic/intelligence/reports/expire`

## Rollout Plan
1. Deploy code with additive schema models (no breaking contract changes).
2. Validate phase-2 endpoints in staging using synthetic players/ships/sectors.
3. Enable scheduled cleanup path (`/api/strategic/intelligence/reports/expire`) via operational task.
4. Monitor claim approval/rejection ratios and intelligence report volume.

## EF Migration Steps
1. Generate migration:
   - `dotnet ef migrations add StrategicSystemsPhase2 --project src/Data --startup-project src/API`
2. Review generated SQL for FK/index correctness.
3. Apply in staging:
   - `dotnet ef database update --project src/Data --startup-project src/API`
4. Apply to production with pre-deployment PostgreSQL backup.

## Rollback
- Restore latest PostgreSQL backup artifact.
- Re-run resilience verification scripts from `docs/resilience-runbook.md`.
