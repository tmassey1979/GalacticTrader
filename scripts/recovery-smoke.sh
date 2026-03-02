#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COMPOSE_FILE="$ROOT_DIR/docker-compose.yml"

"$ROOT_DIR/scripts/backup-postgres.sh"
"$ROOT_DIR/scripts/backup-redis.sh"

latest_pg_backup="$(ls -1t "$ROOT_DIR"/backups/postgres/*.sql.gz | head -n 1)"
latest_redis_backup="$(ls -1t "$ROOT_DIR"/backups/redis/*.rdb | head -n 1)"
restore_db="galactic_restore_check_$(date +%Y%m%d%H%M%S)"

echo "Restoring PostgreSQL backup into $restore_db..."
docker compose -f "$COMPOSE_FILE" exec -T postgres psql -U galactic_admin -d postgres -c "DROP DATABASE IF EXISTS $restore_db;" >/dev/null
docker compose -f "$COMPOSE_FILE" exec -T postgres psql -U galactic_admin -d postgres -c "CREATE DATABASE $restore_db;" >/dev/null
gunzip -c "$latest_pg_backup" | docker compose -f "$COMPOSE_FILE" exec -T postgres psql -U galactic_admin -d "$restore_db" >/dev/null
docker compose -f "$COMPOSE_FILE" exec -T postgres psql -U galactic_admin -d "$restore_db" -c "SELECT 1;" >/dev/null
docker compose -f "$COMPOSE_FILE" exec -T postgres psql -U galactic_admin -d postgres -c "DROP DATABASE $restore_db;" >/dev/null

if [[ ! -s "$latest_redis_backup" ]]; then
  echo "Redis backup artifact is empty: $latest_redis_backup"
  exit 1
fi

echo "Recovery smoke checks passed."
