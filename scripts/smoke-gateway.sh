#!/usr/bin/env bash
set -euo pipefail

gateway_url="${GATEWAY_URL:-http://localhost:8081}"

check_status() {
  local method="$1"
  local path="$2"
  local expected="$3"
  local body="${4:-}"

  local url="${gateway_url}${path}"
  local status

  if [[ -n "$body" ]]; then
    status="$(curl -sS -o /dev/null -w "%{http_code}" -X "$method" -H "Content-Type: application/json" -d "$body" "$url")"
  else
    status="$(curl -sS -o /dev/null -w "%{http_code}" -X "$method" "$url")"
  fi

  if [[ "$status" != "$expected" ]]; then
    echo "Expected ${method} ${path} => ${expected}, got ${status}" >&2
    exit 1
  fi

  echo "OK ${method} ${path} => ${status}"
}

check_status "GET" "/health/live" "200"
check_status "POST" "/api/auth/register" "400" "{}"
check_status "GET" "/api/navigation/sectors" "401"
check_status "GET" "/metrics" "200"

echo "Gateway smoke checks passed."
