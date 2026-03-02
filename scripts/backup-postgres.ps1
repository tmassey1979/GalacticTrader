$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$backupDir = Join-Path $root 'backups\postgres'
$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$backupFile = Join-Path $backupDir "galactictrader_$timestamp.sql.zip"

New-Item -ItemType Directory -Force -Path $backupDir | Out-Null

Write-Host "Creating PostgreSQL backup: $backupFile"

$tempSql = Join-Path $env:TEMP "galactictrader_$timestamp.sql"
docker compose -f (Join-Path $root 'docker-compose.yml') exec -T postgres `
  pg_dump -U galactic_admin -d galactictrader | Out-File -FilePath $tempSql -Encoding utf8

if (-not (Test-Path $tempSql) -or (Get-Item $tempSql).Length -eq 0) {
    throw 'Backup failed: SQL dump is empty.'
}

Compress-Archive -Path $tempSql -DestinationPath $backupFile -Force
Remove-Item -Force $tempSql

Write-Host 'PostgreSQL backup complete.'
