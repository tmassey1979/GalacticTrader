#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COMPOSE_FILE="$ROOT_DIR/docker-compose.yml"
SERVICES=(postgres redis keycloak prometheus grafana vault api gateway)

for service in "${SERVICES[@]}"; do
  container_id="$(docker compose -f "$COMPOSE_FILE" ps -q "$service")"
  if [[ -z "$container_id" ]]; then
    echo "Service $service is not running."
    exit 1
  fi

  state="$(docker inspect -f '{{.State.Status}}' "$container_id")"
  if [[ "$state" != "running" ]]; then
    echo "Service $service is not running (state=$state)."
    exit 1
  fi

  health="$(docker inspect -f '{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}' "$container_id")"
  if [[ "$health" != "healthy" ]]; then
    echo "Service $service is not healthy (health=$health)."
    exit 1
  fi

  restart_policy="$(docker inspect -f '{{.HostConfig.RestartPolicy.Name}}' "$container_id")"
  if [[ "$restart_policy" != "unless-stopped" ]]; then
    echo "Service $service restart policy is '$restart_policy' (expected 'unless-stopped')."
    exit 1
  fi

  echo "OK $service: state=$state health=$health restart=$restart_policy"
done

echo "Resilience verification complete."
