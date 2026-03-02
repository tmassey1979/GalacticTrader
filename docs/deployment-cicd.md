# CI/CD and Deployment

## Workflows
- `dotnet-ci.yml`: runs restore/build/test on pushes and pull requests.
- `docker-publish.yml`: builds and pushes `ghcr.io/<owner>/galactictrader-api` and `ghcr.io/<owner>/galactictrader-gateway` on `main` and tags.
- `deploy.yml`: manual deployment orchestration (`staging` or `production`) with optional rollback tag.

## Registry
- Container registry: GitHub Container Registry (`ghcr.io`).
- Image naming:
  - `ghcr.io/<owner>/galactictrader-api:<tag>`
  - `ghcr.io/<owner>/galactictrader-gateway:<tag>`

## Gateway Smoke Checks
After deploying the stack:

```bash
./scripts/smoke-gateway.sh
```

PowerShell:

```powershell
./scripts/smoke-gateway.ps1
```

See `docs/api-gateway.md` for route policy and runtime configuration details.

## Staging Deployment
1. Copy `infrastructure/staging.env.example` to `infrastructure/staging.env`.
2. Set secrets (`DB_PASSWORD`, `KEYCLOAK_PASSWORD`, `GRAFANA_PASSWORD`).
3. Optional: configure Vault settings (`VAULT_ENABLED`, `VAULT_ADDRESS`, `VAULT_TOKEN`, `VAULT_PATH`).
4. Run:
```bash
./scripts/deploy-staging.sh <image_tag>
```

## Production Deployment
1. Copy `infrastructure/production.env.example` to `infrastructure/production.env`.
2. Set production secrets and approved image tag.
3. Configure Vault (`VAULT_ENABLED=true`) and provide a production-scoped token.
4. Run:
```bash
./scripts/deploy-production.sh <image_tag>
```

## Rollback Procedure
Run rollback using a previously published image tag:
```bash
./scripts/rollback.sh <staging|production> <previous_image_tag>
```

Use `--dry-run` on deploy and rollback scripts to validate command construction without applying changes.

## Vault Secrets
- API supports HashiCorp Vault secret bootstrap at startup.
- See `docs/vault-secrets.md` for configuration and local seeding commands.
