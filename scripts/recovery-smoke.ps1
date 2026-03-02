$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$composeFile = Join-Path $root "docker-compose.yml"

& (Join-Path $root "scripts\backup-postgres.ps1")
& (Join-Path $root "scripts\backup-redis.ps1")

$latestPostgresBackup = Get-ChildItem (Join-Path $root "backups\postgres\*.sql.zip") | Sort-Object LastWriteTime -Descending | Select-Object -First 1
$latestRedisBackup = Get-ChildItem (Join-Path $root "backups\redis\*.rdb") | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if (-not $latestPostgresBackup) {
    throw "No PostgreSQL backup artifact found."
}

if (-not $latestRedisBackup) {
    throw "No Redis backup artifact found."
}

$tempExtractDir = Join-Path $env:TEMP ("gt_restore_" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $tempExtractDir | Out-Null
Expand-Archive -Path $latestPostgresBackup.FullName -DestinationPath $tempExtractDir -Force
$sqlFile = Get-ChildItem $tempExtractDir -Filter "*.sql" | Select-Object -First 1
if (-not $sqlFile) {
    throw "No SQL file found inside $($latestPostgresBackup.FullName)"
}

$restoreDb = "galactic_restore_check_{0}" -f (Get-Date -Format "yyyyMMddHHmmss")

Write-Host "Restoring PostgreSQL backup into $restoreDb..."
docker compose -f $composeFile exec -T postgres psql -U galactic_admin -d postgres -c "DROP DATABASE IF EXISTS $restoreDb;" | Out-Null
docker compose -f $composeFile exec -T postgres psql -U galactic_admin -d postgres -c "CREATE DATABASE $restoreDb;" | Out-Null
Get-Content -Path $sqlFile.FullName | docker compose -f $composeFile exec -T postgres psql -U galactic_admin -d $restoreDb | Out-Null
docker compose -f $composeFile exec -T postgres psql -U galactic_admin -d $restoreDb -c "SELECT 1;" | Out-Null
docker compose -f $composeFile exec -T postgres psql -U galactic_admin -d postgres -c "DROP DATABASE $restoreDb;" | Out-Null

if ((Get-Item $latestRedisBackup.FullName).Length -eq 0) {
    throw "Redis backup artifact is empty: $($latestRedisBackup.FullName)"
}

Remove-Item -Path $tempExtractDir -Recurse -Force

Write-Host "Recovery smoke checks passed."
