# Backup and Resilience Runbook

This runbook covers backup automation, resilience verification, graceful shutdown, and recovery smoke tests.

## Nightly Backups
- GitHub Actions workflow: `.github/workflows/nightly-backups.yml`
- Schedule: daily at `04:00 UTC`
- Artifacts:
  - `backups/postgres/*.sql.gz`
  - `backups/redis/*.rdb`

Manual execution:
- Linux/macOS:
  - `./scripts/backup-postgres.sh`
  - `./scripts/backup-redis.sh`
- PowerShell:
  - `./scripts/backup-postgres.ps1`
  - `./scripts/backup-redis.ps1`

## Resilience Verification
Verify service health checks and restart policies:
- Linux/macOS: `./scripts/verify-resilience.sh`
- PowerShell: `./scripts/verify-resilience.ps1`

Expected result per service: `running`, `healthy`, and restart policy `unless-stopped`.

## Graceful Tick-Engine Shutdown Procedure
To avoid starting new ticks while draining active API work:
1. Stop gateway first (blocks new ingress).
2. Stop API with timeout to allow in-flight operations to finish.

Use:
- Linux/macOS: `./scripts/graceful-shutdown.sh 30`
- PowerShell: `./scripts/graceful-shutdown.ps1 -TimeoutSeconds 30`

## Recovery Smoke Tests
Run backup + restore validation:
- Linux/macOS: `./scripts/recovery-smoke.sh`
- PowerShell: `./scripts/recovery-smoke.ps1`

Validation includes:
- PostgreSQL backup generation and restore into a temporary database.
- Basic query execution against restored DB.
- Redis backup artifact existence and non-empty file check.
