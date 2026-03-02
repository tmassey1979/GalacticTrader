$ErrorActionPreference = "Stop"

param(
    [int]$TimeoutSeconds = 30
)

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$composeFile = Join-Path $root "docker-compose.yml"

Write-Host "Stopping gateway first to drain incoming traffic..."
docker compose -f $composeFile stop --timeout $TimeoutSeconds gateway | Out-Null

Write-Host "Stopping API service with graceful timeout ($TimeoutSeconds s)..."
docker compose -f $composeFile stop --timeout $TimeoutSeconds api | Out-Null

Write-Host "Graceful shutdown procedure complete."
