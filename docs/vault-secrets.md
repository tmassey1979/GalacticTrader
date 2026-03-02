# Vault Secrets Management

Galactic Trader API can load configuration secrets from HashiCorp Vault at startup.

## How It Works
- When `Vault__Enabled=true`, API reads a KV secret path and injects key/value pairs into application configuration.
- Vault keys can use either `:` or `__` separators; `__` is normalized to `:` for .NET config binding.
- Vault-loaded values override earlier config providers for matching keys.

## Required Settings
- `Vault__Enabled=true`
- `Vault__Address=http://vault:8200` (or your Vault URL)
- `Vault__Token=<vault token with read access>`
- `Vault__Mount=secret`
- `Vault__Path=galactictrader/api`

Optional:
- `Vault__KvVersion=2` (default `2`)
- `Vault__FailFast=true` (default `true`)

## Local Development (Docker Compose)
1. Enable Vault in `.env`:
```env
VAULT_ENABLED=true
VAULT_DEV_ROOT_TOKEN=root
VAULT_MOUNT=secret
VAULT_PATH=galactictrader/api
VAULT_KV_VERSION=2
```
2. Start stack:
```bash
docker compose up -d vault api postgres redis keycloak
```
3. Seed secrets:
```bash
docker exec -e VAULT_ADDR=http://127.0.0.1:8200 -e VAULT_TOKEN=root galactictrader-vault \
  vault kv put secret/galactictrader/api \
  ConnectionStrings__Default="Host=postgres;Port=5432;Database=galactictrader;Username=galactic_admin;Password=ChangeMeInProduction" \
  Redis__Connection="redis:6379" \
  Keycloak__ServerUrl="http://keycloak:8080" \
  Keycloak__Realm="galactictrader"
```

## Production Guidance
- Use short-lived Vault tokens via your platform identity flow where possible.
- Scope policies to read-only on the specific API secret path.
- Set `Vault__FailFast=true` to block startup if secret bootstrap fails.
- Rotate tokens and secret values regularly.
