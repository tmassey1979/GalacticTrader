#!/usr/bin/env bash
set -euo pipefail

TARGET_ENV="${1:-staging}"
ROLLBACK_TAG="${2:-}"
MODE="${3:-}"

if [[ -z "$ROLLBACK_TAG" ]]; then
  echo "usage: ./scripts/rollback.sh <staging|production> <image_tag> [--dry-run]"
  exit 1
fi

DRY_RUN=false
if [[ "$MODE" == "--dry-run" ]]; then
  DRY_RUN=true
fi

if [[ "$TARGET_ENV" == "staging" ]]; then
  SCRIPT="./scripts/deploy-staging.sh"
elif [[ "$TARGET_ENV" == "production" ]]; then
  SCRIPT="./scripts/deploy-production.sh"
else
  echo "invalid target environment: $TARGET_ENV"
  exit 1
fi

echo "[rollback] target=${TARGET_ENV} tag=${ROLLBACK_TAG}"
if [[ "$DRY_RUN" == true ]]; then
  "$SCRIPT" "$ROLLBACK_TAG" --dry-run
else
  "$SCRIPT" "$ROLLBACK_TAG"
fi
