$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$backupDir = Join-Path $root 'backups\redis'
$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$backupFile = Join-Path $backupDir "redis_$timestamp.rdb"

New-Item -ItemType Directory -Force -Path $backupDir | Out-Null

Write-Host 'Triggering Redis snapshot...'
docker compose -f (Join-Path $root 'docker-compose.yml') exec -T redis redis-cli SAVE | Out-Null

Write-Host "Copying Redis snapshot: $backupFile"
docker compose -f (Join-Path $root 'docker-compose.yml') cp redis:/data/dump.rdb $backupFile | Out-Null

if (-not (Test-Path $backupFile) -or (Get-Item $backupFile).Length -eq 0) {
    throw 'Backup failed: Redis dump file is empty.'
}

Write-Host 'Redis backup complete.'
