#!/usr/bin/env bash
set -euo pipefail

IMAGE_TAG="${1:-latest}"
MODE="${2:-}"
DRY_RUN=false
if [[ "$MODE" == "--dry-run" ]]; then
  DRY_RUN=true
fi

OWNER="${IMAGE_OWNER:-${GITHUB_REPOSITORY_OWNER:-tmassey1979}}"
API_IMAGE_PATH="${API_IMAGE_PATH:-galactictrader/api}"
API_IMAGE="ghcr.io/${OWNER}/${API_IMAGE_PATH}:${IMAGE_TAG}"
ENV_FILE="${ENV_FILE:-infrastructure/staging.env}"

if [[ ! -f "$ENV_FILE" ]]; then
  ENV_FILE="infrastructure/staging.env.example"
fi

CMD="API_IMAGE=${API_IMAGE} docker compose --env-file ${ENV_FILE} pull api && API_IMAGE=${API_IMAGE} docker compose --env-file ${ENV_FILE} up -d api"

echo "[staging] image=${API_IMAGE}"
echo "[staging] env_file=${ENV_FILE}"
if [[ "$DRY_RUN" == true ]]; then
  echo "[staging] dry-run command: ${CMD}"
else
  eval "$CMD"
fi
