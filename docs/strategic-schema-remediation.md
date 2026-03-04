# Strategic Schema Remediation Runbook

Use this runbook for legacy PostgreSQL environments created before managed EF migrations were adopted.

## When To Run

- You are upgrading to builds that use migration-based startup (`Database.Migrate()`).
- Existing database was initialized with `EnsureCreated` and may not contain all strategic phase 1/2 objects.
- You need to align strategic schema and baseline migration history before app startup migration execution.

## Script

- SQL script: `scripts/strategic-schema-remediation.sql`
- Scope:
  - Ensures strategic phase 1/2 tables exist.
  - Ensures required strategic indexes and FK constraints exist.
  - Ensures `__EFMigrationsHistory` exists.
  - Inserts baseline migration history row (`20260304164301_InitialSchemaBaseline`) only if required base + strategic tables exist.

## Prerequisites (Backup First)

1. Confirm target database and credentials.
2. Take a pre-change backup:
```bash
pg_dump --format=custom --file=backup-pre-strategic-remediation.dump "<postgres-connection-string>"
```
3. Verify backup artifact integrity and retention policy.

## Execute Remediation

```bash
psql "<postgres-connection-string>" -f scripts/strategic-schema-remediation.sql
```

The script is idempotent and safe for repeated execution.

## Verification Queries

### Required Strategic Tables

```sql
SELECT table_name
FROM information_schema.tables
WHERE table_schema = 'public'
  AND table_name IN (
    'SectorVolatilityCycles',
    'CorporateWars',
    'InfrastructureOwnerships',
    'TerritoryDominances',
    'InsurancePolicies',
    'InsuranceClaims',
    'IntelligenceNetworks',
    'IntelligenceReports'
  )
ORDER BY table_name;
```

### Migration Baseline Stamp

```sql
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20260304164301_InitialSchemaBaseline';
```

### Strategic FK/Index Spot Checks

```sql
SELECT conname
FROM pg_constraint
WHERE conname IN (
  'FK_TerritoryDominances_Factions_FactionId',
  'FK_InsuranceClaims_InsurancePolicies_PolicyId',
  'FK_IntelligenceReports_IntelligenceNetworks_NetworkId'
)
ORDER BY conname;

SELECT indexname
FROM pg_indexes
WHERE schemaname = 'public'
  AND indexname IN (
    'IX_TerritoryDominances_FactionId',
    'IX_InsurancePolicies_PlayerId_IsActive',
    'IX_IntelligenceReports_NetworkId_SectorId_DetectedAt'
  )
ORDER BY indexname;
```

## Post-Remediation API Checks

After deploying the API build, verify representative strategic endpoints:

```bash
curl -fsS http://localhost:8080/api/strategic/volatility
curl -fsS "http://localhost:8080/api/strategic/corporate-wars?activeOnly=true"
curl -fsS http://localhost:8080/api/strategic/infrastructure
curl -fsS http://localhost:8080/api/strategic/territory-dominance
```

For player-scoped strategic endpoints, verify using owner/admin bearer token flow.

## Rollback

1. Stop API writes.
2. Restore backup:
```bash
pg_restore --clean --if-exists --dbname "<postgres-connection-string>" backup-pre-strategic-remediation.dump
```
3. Validate core API health before re-enabling traffic.
