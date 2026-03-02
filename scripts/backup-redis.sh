#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BACKUP_DIR="$ROOT_DIR/backups/redis"
TIMESTAMP="$(date +%Y%m%d_%H%M%S)"
BACKUP_FILE="$BACKUP_DIR/redis_${TIMESTAMP}.rdb"

mkdir -p "$BACKUP_DIR"

echo "Triggering Redis snapshot..."
docker compose -f "$ROOT_DIR/docker-compose.yml" exec -T redis redis-cli SAVE >/dev/null

echo "Copying Redis snapshot: $BACKUP_FILE"
docker compose -f "$ROOT_DIR/docker-compose.yml" cp redis:/data/dump.rdb "$BACKUP_FILE"

if [[ ! -s "$BACKUP_FILE" ]]; then
  echo "Backup failed: Redis dump file is empty."
  exit 1
fi

echo "Redis backup complete."
