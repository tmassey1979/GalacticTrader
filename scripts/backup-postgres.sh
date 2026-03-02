#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BACKUP_DIR="$ROOT_DIR/backups/postgres"
TIMESTAMP="$(date +%Y%m%d_%H%M%S)"
BACKUP_FILE="$BACKUP_DIR/galactictrader_${TIMESTAMP}.sql.gz"

mkdir -p "$BACKUP_DIR"

echo "Creating PostgreSQL backup: $BACKUP_FILE"
docker compose -f "$ROOT_DIR/docker-compose.yml" exec -T postgres \
  pg_dump -U galactic_admin -d galactictrader | gzip > "$BACKUP_FILE"

if [[ ! -s "$BACKUP_FILE" ]]; then
  echo "Backup failed: output file is empty."
  exit 1
fi

echo "PostgreSQL backup complete."
