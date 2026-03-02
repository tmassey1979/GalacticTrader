$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$composeFile = Join-Path $root "docker-compose.yml"
$services = @("postgres", "redis", "keycloak", "prometheus", "grafana", "vault", "api", "gateway")

foreach ($service in $services) {
    $containerId = (docker compose -f $composeFile ps -q $service).Trim()
    if ([string]::IsNullOrWhiteSpace($containerId)) {
        throw "Service $service is not running."
    }

    $state = docker inspect -f "{{.State.Status}}" $containerId
    if ($state -ne "running") {
        throw "Service $service is not running (state=$state)."
    }

    $health = docker inspect -f "{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}" $containerId
    if ($health -ne "healthy") {
        throw "Service $service is not healthy (health=$health)."
    }

    $restartPolicy = docker inspect -f "{{.HostConfig.RestartPolicy.Name}}" $containerId
    if ($restartPolicy -ne "unless-stopped") {
        throw "Service $service restart policy is '$restartPolicy' (expected 'unless-stopped')."
    }

    Write-Host "OK $service`: state=$state health=$health restart=$restartPolicy"
}

Write-Host "Resilience verification complete."
