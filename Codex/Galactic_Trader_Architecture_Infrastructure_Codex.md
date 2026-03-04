# GALACTIC TRADER

## Architecture & Infrastructure Codex

### Version 2.0 (Current-State Aligned)

## 1. Scope

This codex describes the architecture as it exists today, and separately marks forward-looking target architecture.

## 2. Current State (Implemented)

### Runtime and Language

- Backend runtime: .NET 9
- API host: ASP.NET Core Minimal API (`src/API`)
- Data access: EF Core 9 + Npgsql provider
- Primary client: WPF desktop application (`src/Desktop`)
- Optional edge service: gateway (`src/Gateway`)

### Service Topology

Current topology is a modular monolith for gameplay backend logic:

- One API process exposes endpoints for navigation, combat, economy, market, NPC, fleet, reputation, strategic systems, telemetry, and communication.
- Domain logic is organized by service classes under `src/Services`, but executes in-process with the API host.
- PostgreSQL is the primary relational database in relational environments.
- In-memory EF provider is used only for local/test fallback scenarios where relational persistence is not configured.

### Infrastructure

- Local/dev orchestration: Docker Compose (`docker-compose.yml`)
- Core runtime services:
  - API (`:8080`)
  - Gateway (`:8081`)
  - PostgreSQL
  - Redis
  - Keycloak
  - Prometheus
  - Grafana
  - Vault (optional)

### Auth and Authorization

- Primary app login path: `/api/auth/login`
- Keycloak credential login is enabled only when required config is explicitly present (`Keycloak__ServerUrl`, `Keycloak__Realm`, `Keycloak__ClientId`).
- Startup logs report resolved auth mode and fallback policy.
- `X-Admin-Key` auth is deprecated and default-off outside Development; bearer admin-role auth is the target path.

### Database Lifecycle

- Relational startup uses managed migrations (`Database.Migrate()`).
- Non-relational test/dev fallback uses `EnsureCreated()`.
- Strategic schema smoke check validates strategic tables after relational migration.

## 3. Monolith vs Microservices Boundary

### Current Boundary

- Implemented: modular monolith backend (single API host process + in-process service modules).
- Not implemented as independently deployable services: navigation/combat/economy/market/etc.

### Target Boundary (Roadmap)

The following are candidate future extraction boundaries (not current production shape):

- Dedicated simulation workers (combat/economy/NPC cycles)
- Independent communication/signaling service
- Separate telemetry aggregation path

These roadmap items require explicit issue-driven implementation and are not assumed active today.

## 4. Observability

Implemented metric families include:

- API latency (`api_request_duration_seconds`)
- Route calculation and combat tick durations
- Database duration envelope metric
- Strategic intelligence expiry counters/histograms
- Legacy admin-key deprecation usage counter (`admin_legacy_key_auth_attempts_total`)

Dashboards and operational flow are documented under `docs/deployment-cicd.md` and related runbooks.

## 5. Deployment States

### Current Deployable States

- Local compose stack
- Staging and production script-driven deployments
- Container images published through CI

### Target States (Roadmap)

Kubernetes deployment options and provider comparisons are tracked as roadmap issues and may be blocked depending on project state.

## 6. Documentation Contracts

When architecture changes, update these sources together:

- `README.md` (runtime + topology summary)
- `docs/architecture.md` (operational architecture diagrams)
- `Codex/Galactic_Trader_Architecture_Infrastructure_Codex.md` (this file)

## 7. References

- `README.md`
- `docs/architecture.md`
- `docs/deployment-cicd.md`
- `docs/admin-auth-deprecation.md`
- `docs/strategic-schema-remediation.md`
