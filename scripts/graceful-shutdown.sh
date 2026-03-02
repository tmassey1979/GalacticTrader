#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TIMEOUT_SECONDS="${1:-30}"

echo "Stopping gateway first to drain incoming traffic..."
docker compose -f "$ROOT_DIR/docker-compose.yml" stop --timeout "$TIMEOUT_SECONDS" gateway

echo "Stopping API service with graceful timeout (${TIMEOUT_SECONDS}s)..."
docker compose -f "$ROOT_DIR/docker-compose.yml" stop --timeout "$TIMEOUT_SECONDS" api

echo "Graceful shutdown procedure complete."
