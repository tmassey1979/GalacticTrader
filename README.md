# Galactic Trader - Backend and Desktop Development Guide

A deterministic, server-authoritative economic space simulation built with .NET 9, PostgreSQL, Redis, and Keycloak.

The primary client is now a WPF desktop UI with a 3D tactical starmap in `src/Desktop`.

## Prerequisites

- .NET 9 SDK or later
- Docker and Docker Compose
- PostgreSQL 16+ (or use Docker)
- Redis 7+ (or use Docker)
- Git

## Quick Start with Docker

1. **Clone the repository**
   ```bash
   git clone https://github.com/tmassey1979/GalacticTrader.git
   cd GalacticTrader
   ```

2. **Setup environment variables**
   ```bash
   cp .env.example .env
   # Edit .env with your desired passwords
   ```

3. **Start services**
   ```bash
   docker-compose up -d
   ```

4. **Build and run the API**
   ```bash
   dotnet build
   dotnet run --project src/API
   ```

   The API will be available at: `http://localhost:8080`

## Services

### Infrastructure Services

- **PostgreSQL** (port 5432) - Primary database
- **Redis** (port 6379) - Caching and session storage
- **Keycloak** (port 8180) - Identity provider with OAuth2/OIDC
- **Prometheus** (port 9090) - Metrics collection
- **Grafana** (port 3000) - Dashboard visualization
- **Vault** (port 8200) - Secret bootstrap for API/Gateway
- **Gateway** (port 8081) - API entrypoint with JWT validation and rate limiting
- **API** (port 8080) - Backend application service behind gateway

### Backend Services

- **Navigation Service** - Route calculation and autopilot
- **Combat Service** - Deterministic tick-based battles
- **Economy Service** - Dynamic pricing engine
- **Market Service** - Trade transactions
- **NPC Service** - Autonomous agent behavior
- **Fleet Service** - Ship and crew management
- **Communication Service** - Text and voice channels
- **Telemetry Service** - Metrics and observability

### Desktop Client

- **WPF Desktop UI** - Command interface with `Viewport3D` starmap
- **Animated Splash Screen** - 3D ship fly-in with logo reveal and terminal-style boot sequence

## Project Structure

```
GalacticTrader/
├── src/
│   ├── API/                          # Web API Gateway
│   ├── Services/                     # Business logic services
│   ├── Data/                         # Entity Framework Core & repositories
│   └── Shared/                       # Shared utilities
├── tests/                            # Unit and integration tests
├── infrastructure/                   # Docker, k8s configs
├── Codex/                            # Architecture documentation
├── Dockerfile                        # API container config
├── docker-compose.yml                # Development environment
├── global.json                       # .NET SDK version
└── GalacticTrader.sln               # Solution file
```

## Development Workflow

### Building the Solution

```bash
# Restore dependencies
dotnet restore

# Build debug version
dotnet build

# Build release version
dotnet build -c Release
```

### Running the API

```bash
# Development mode with hot reload
dotnet watch run --project src/API

# Production mode
dotnet run --project src/API -c Release
```

### Running the Gateway

```bash
dotnet run --project src/Gateway
```

### Running the Desktop UI

```bash
dotnet run --project src/Desktop
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

```bash
# Run integration tests only
dotnet test tests/GalacticTrader.IntegrationTests

# Run benchmark suite
dotnet run --project tests/GalacticTrader.Benchmarks -c Release -- --job short

# Run Playwright E2E critical flows
cd tests/e2e && npm install && npm test
```

## Database Setup

### Entity Framework Core Migrations

```bash
# Create a new migration
dotnet ef migrations add MigrationName --project src/Data --startup-project src/API

# Apply pending migrations
dotnet ef database update --project src/Data --startup-project src/API

# Remove last migration
dotnet ef migrations remove --project src/Data --startup-project src/API
```

## API Documentation

Once running, view the Swagger documentation at:
- http://localhost:8080/swagger
- Through gateway: http://localhost:8081/swagger
- See [docs/api-openapi.md](docs/api-openapi.md) for auth, examples, error codes, and SDK generation.
- Gateway routes and smoke checks: [docs/api-gateway.md](docs/api-gateway.md)

## Configuration

### Environment Variables

See `.env.example` for all configurable options:

- `ASPNETCORE_ENVIRONMENT` - Development/Staging/Production
- `ConnectionStrings__Default` - PostgreSQL connection string
- `Redis__Connection` - Redis connection string
- `Keycloak__ServerUrl` - Keycloak server URL
- `Vault__Enabled` - Enable HashiCorp Vault secret bootstrap
- `Vault__Address` / `Vault__Token` / `Vault__Path` - Vault connection and secret path

See [docs/vault-secrets.md](docs/vault-secrets.md) for Vault setup and seeding steps.

## Docker Commands

```bash
# Start all services
docker-compose up -d

# Stop services
docker-compose down

# View logs
docker-compose logs -f api

# Rebuild containers
docker-compose build --no-cache

# Remove volumes (warning: deletes data)
docker-compose down -v
```

## Backup and Resilience

- Nightly backup workflow: `.github/workflows/nightly-backups.yml`
- Runbook: [docs/resilience-runbook.md](docs/resilience-runbook.md)
- Local verification scripts:
  - `./scripts/verify-resilience.sh` or `./scripts/verify-resilience.ps1`
  - `./scripts/recovery-smoke.sh` or `./scripts/recovery-smoke.ps1`

## Monitoring

- **Prometheus**: http://localhost:9090 - View raw metrics
- **Grafana**: http://localhost:3000 - View dashboards (admin/admin)

## Performance Targets

- Combat tick processing: < 50ms
- Route calculation: < 20ms average
- API p95 latency: < 150ms
- Support 1000+ concurrent users
- Deterministic simulation under load

## Authentication

Production authentication is expected through Keycloak OAuth2/OIDC.

For development and test automation, use the built-in auth endpoints:
- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/auth/validate`

## Strategic Systems Phase 1

Strategic APIs are available under `/api/strategic` for:
- sector volatility cycles
- corporate wars
- infrastructure ownership
- territory dominance recalculation and ranking

Migration and rollout guidance:
- [docs/strategic-systems-migration-plan.md](docs/strategic-systems-migration-plan.md)

## Contributing

1. Create a feature branch
2. Make changes and add tests
3. Run tests locally: `dotnet test`
4. Commit with descriptive messages
5. Push to GitHub
6. Open a Pull Request

## Architecture Codex

For complete architecture documentation, see:
- [Full Codex](Codex/Galactic_Trader_Full_Codex.md)
- [Architecture & Infrastructure](Codex/Galactic_Trader_Architecture_Infrastructure_Codex.md)
- [User Interface](Codex/Galactic_Trader_User_Interface_Codex.md)
- [Current architecture guide](docs/architecture.md)

## License

Proprietary - All rights reserved

## Support

For issues and questions, open a GitHub issue or check the documentation.
