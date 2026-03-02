#!/usr/bin/env bash
set -euo pipefail

API_BASE_URL="${1:-http://localhost:8080}"
OUTPUT_DIR="${2:-generated-clients}"

mkdir -p "${OUTPUT_DIR}"
SPEC_PATH="${OUTPUT_DIR}/openapi.json"

curl -fsSL "${API_BASE_URL}/swagger/v1/swagger.json" -o "${SPEC_PATH}"

npx @openapitools/openapi-generator-cli generate -i "${SPEC_PATH}" -g typescript-fetch -o "${OUTPUT_DIR}/typescript"
npx @openapitools/openapi-generator-cli generate -i "${SPEC_PATH}" -g csharp -o "${OUTPUT_DIR}/csharp"

echo "SDK generation complete in ${OUTPUT_DIR}"
