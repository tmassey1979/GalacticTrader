# CI/CD and Deployment

## Workflows
- `dotnet-ci.yml`: runs restore/build/test on pushes and pull requests.
- `docker-compose-smoke.yml`: brings up backend services with Docker Compose and runs gateway smoke checks on pushes and pull requests.
- `docker-publish.yml`: builds and pushes `ghcr.io/<owner>/galactictrader/api` and `ghcr.io/<owner>/galactictrader/gateway` on `main` and tags.
- `deploy.yml`: manual deployment orchestration (`staging` or `production`) with optional rollback tag.
- `nightly-backups.yml`: scheduled PostgreSQL and Redis backup job with retained artifacts.
- `performance-benchmarks.yml`: scheduled/manual BenchmarkDotNet run with uploaded artifacts.
- `unity-client-build.yml`: Unity Windows client build/package pipeline that publishes both a zip and installer artifact when a valid Unity project is present.

## Registry
- Container registry: GitHub Container Registry (`ghcr.io`).
- Image naming:
  - `ghcr.io/<owner>/galactictrader/api:<tag>`
  - `ghcr.io/<owner>/galactictrader/gateway:<tag>`

## Unity Client Artifacts

- Workflow: `unity-client-build.yml`
- Artifacts produced for Windows:
  - `unity-windows-zip`
  - `unity-windows-installer`
- Installer definition file:
  - `infrastructure/unity/installer/GalacticTraderUnity.iss`

## Gateway Smoke Checks
After deploying the stack:

```bash
./scripts/smoke-gateway.sh
```

PowerShell:

```powershell
./scripts/smoke-gateway.ps1
```

See `docs/api-gateway.md` for route policy and runtime configuration details.

## Resilience Runbook
Operational procedures for backups, restore smoke tests, and graceful API shutdown:

- `docs/resilience-runbook.md`

## Staging Deployment
1. Copy `infrastructure/staging.env.example` to `infrastructure/staging.env`.
2. Set secrets (`DB_PASSWORD`, `KEYCLOAK_PASSWORD`, `GRAFANA_PASSWORD`).
3. Optional: configure Vault settings (`VAULT_ENABLED`, `VAULT_ADDRESS`, `VAULT_TOKEN`, `VAULT_PATH`).
4. Run:
```bash
./scripts/deploy-staging.sh <image_tag>
```

## Production Deployment
1. Copy `infrastructure/production.env.example` to `infrastructure/production.env`.
2. Set production secrets and approved image tag.
3. Configure Vault (`VAULT_ENABLED=true`) and provide a production-scoped token.
4. Run:
```bash
./scripts/deploy-production.sh <image_tag>
```

## Rollback Procedure
Run rollback using a previously published image tag:
```bash
./scripts/rollback.sh <staging|production> <previous_image_tag>
```

Use `--dry-run` on deploy and rollback scripts to validate command construction without applying changes.

## Database Migrations (PostgreSQL)

GalacticTrader now uses managed EF Core migrations for relational deployments.

- API startup behavior:
  - PostgreSQL/relational providers: apply pending migrations via `Database.Migrate()`.
  - In-memory/non-relational providers (test/dev-only): use `EnsureCreated()`.
- Startup smoke check validates required strategic tables after migration:
  - `SectorVolatilityCycles`
  - `CorporateWars`
  - `InfrastructureOwnerships`
  - `TerritoryDominances`
  - `InsurancePolicies`
  - `InsuranceClaims`
  - `IntelligenceNetworks`
  - `IntelligenceReports`

### Pre-Deployment Migration Script (Recommended)

Generate an idempotent SQL migration artifact from CI or a release workstation:

```bash
dotnet ef migrations script --idempotent \
  --project src/Data/GalacticTrader.Data.csproj \
  --startup-project src/API/GalacticTrader.API.csproj \
  --output artifacts/migrations/latest-idempotent.sql
```

Apply with your standard PostgreSQL change-control tooling (or allow app startup to apply migrations automatically).

Legacy-environment remediation runbook:
- `docs/strategic-schema-remediation.md`

### Rollback Procedure (Schema)

1. Take a fresh PostgreSQL backup/snapshot.
2. Identify the previous target migration:
```bash
dotnet ef migrations list \
  --project src/Data/GalacticTrader.Data.csproj \
  --startup-project src/API/GalacticTrader.API.csproj
```
3. Revert schema to the selected migration:
```bash
dotnet ef database update <PreviousMigrationName> \
  --project src/Data/GalacticTrader.Data.csproj \
  --startup-project src/API/GalacticTrader.API.csproj
```
4. Redeploy the corresponding application image tag.
5. Re-run gateway smoke checks and strategic endpoint checks.

## Vault Secrets
- API supports HashiCorp Vault secret bootstrap at startup.
- See `docs/vault-secrets.md` for configuration and local seeding commands.

## Admin Auth Deprecation

- Legacy `X-Admin-Key` support is deprecated and default-off outside Development.
- Migration timeline and cutover dates are documented in:
  - `docs/admin-auth-deprecation.md`
