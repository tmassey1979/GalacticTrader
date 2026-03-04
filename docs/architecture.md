# Architecture Documentation

## Status
Complete

## System Architecture Diagram
```mermaid
graph TB
    subgraph Clients
        Unity[Unity Client\nAction-First Gameplay UI]
        External[External Integrations\nSDK Clients]
    end

    subgraph Gateway
        API[ASP.NET Core API\nMinimal API]
    end

    subgraph CoreServices
        NAV[Navigation Service]
        COMBAT[Combat Service]
        ECON[Economy Service]
        MARKET[Market Service]
        NPC[NPC Service]
        FLEET[Fleet Service]
        REP[Reputation Service]
        LB[Leaderboard Service]
        COMM[Communication Service]
        AUTH[Auth Service]
    end

    subgraph DataPlane
        DB[(PostgreSQL / InMemory EF Core)]
        REDIS[(Redis Cache)]
    end

    subgraph Observability
        PROM[Prometheus]
        GRAF[Grafana]
        ALERT[Alert Rules]
    end

    Unity --> API
    External --> API

    API --> AUTH
    API --> NAV
    API --> COMBAT
    API --> ECON
    API --> MARKET
    API --> NPC
    API --> FLEET
    API --> REP
    API --> LB
    API --> COMM

    NAV --> DB
    COMBAT --> DB
    ECON --> DB
    MARKET --> DB
    NPC --> DB
    FLEET --> DB
    REP --> DB
    LB --> DB
    COMM --> DB

    API --> REDIS
    API --> PROM
    PROM --> GRAF
    PROM --> ALERT
```

## Service Interaction Diagram
```mermaid
graph LR
    NAV --> ECON
    NAV --> NPC
    COMBAT --> FLEET
    COMBAT --> REP
    MARKET --> ECON
    MARKET --> LB
    NPC --> NAV
    NPC --> MARKET
    FLEET --> COMBAT
    REP --> LB
    COMM --> REP
```

## Database Schema (ERD)
```mermaid
erDiagram
    PLAYER ||--o{ SHIP : owns
    PLAYER ||--o{ CREW : employs
    PLAYER ||--o{ LEADERBOARD : ranks
    PLAYER ||--o{ REPUTATION : has

    SECTOR ||--o{ ROUTE : source
    SECTOR ||--o{ ROUTE : destination
    SECTOR ||--o{ MARKET : hosts

    MARKET ||--o{ MARKET_LISTING : offers
    COMMODITY ||--o{ MARKET_LISTING : listed
    MARKET_LISTING ||--o{ MARKET_PRICE_HISTORY : tracks

    SHIP ||--o{ SHIP_MODULE : fitted
    SHIP ||--o{ CREW : assigned

    PLAYER ||--o{ TRADE_TRANSACTION : buys
    COMMODITY ||--o{ TRADE_TRANSACTION : traded

    SHIP ||--o{ COMBAT_LOG : attacker
    SHIP ||--o{ COMBAT_LOG : defender

    CHANNEL_MESSAGE {
      guid id PK
      string channel_type
      string channel_key
      guid player_id
      string content
      datetime created_at
    }
```

## Sequence Diagrams

### Trading workflow sequence
```mermaid
sequenceDiagram
    participant Client
    participant API
    participant Economy
    participant Market
    participant DB

    Client->>API: POST /api/economy/tick
    API->>Economy: ProcessMarketTickAsync()
    Economy->>DB: Read/update listings + history
    DB-->>Economy: Persisted
    Economy-->>API: Tick result
    API-->>Client: 200 OK

    Client->>API: POST /api/market/trade
    API->>Market: ExecuteTradeAsync()
    Market->>DB: Validate listing, ship, player, balances
    DB-->>Market: Domain state
    Market->>DB: Write transaction + state mutations
    Market-->>API: Trade result
    API-->>Client: 200/400
```

### Navigation planning sequence
```mermaid
sequenceDiagram
    participant Client
    participant API
    participant Planner
    participant Repo
    participant Cache

    Client->>API: GET /api/navigation/planning/{from}/{to}
    API->>Planner: CalculateRouteAsync(...)
    Planner->>Cache: Get(route-key)
    alt cache hit
        Cache-->>Planner: plan
    else cache miss
        Planner->>Repo: Load sectors/routes
        Repo-->>Planner: graph snapshot
        Planner->>Planner: Dijkstra/A* solve
        Planner->>Cache: Set(route-key)
    end
    Planner-->>API: RoutePlanDto
    API-->>Client: 200/404
```

## Deployment Architecture
```mermaid
graph TB
    GH[GitHub Actions] --> REG[Container Registry]
    GH --> STAGE[Staging Environment]
    GH --> PROD[Production Environment]

    subgraph Runtime Cluster
        API_C[API Container]
        DB_C[(PostgreSQL)]
        REDIS_C[(Redis)]
        PROM_C[Prometheus]
        GRAF_C[Grafana]
    end

    STAGE --> API_C
    PROD --> API_C
    API_C --> DB_C
    API_C --> REDIS_C
    API_C --> PROM_C
    PROM_C --> GRAF_C
```

## Service Responsibilities
- API Gateway: Routing, serialization, endpoint composition, telemetry middleware.
- Auth Service: Registration/login/token validation for dev and E2E workflows.
- Navigation Service: Sector graph storage, route CRUD, Dijkstra/A* planning, autopilot.
- Combat Service: Tick-based deterministic combat simulation and combat log persistence.
- Economy Service: Dynamic price model, market tick updates, volatility and shock handling.
- Market Service: Trade execution, reversal, anti-exploit checks, transaction history.
- NPC Service: Archetype-based decisions, fleet spawn/movement/trading.
- Fleet Service: Ship templates, purchase, module fitting, crew progression, convoy simulation.
- Reputation Service: Faction and alignment standing updates plus decay.
- Leaderboard Service: Rank recalculation, history, position snapshots, resets.
- Communication Service: Channel subscriptions, moderation/rate limits, websocket and voice signaling.

## Scaling Strategy
- API horizontal scaling via stateless app instances.
- Redis for distributed cache/session acceleration.
- Background telemetry refresh isolated from request path.
- Read/write split path ready for PostgreSQL replicas.
- Queue-ready boundaries around combat ticks, market ticks, and NPC cycles.
- Benchmark workflow for recurring regression checks.

## Deployment Guide
- CI/CD flow and scripts: `docs/deployment-cicd.md`
- Staging env template: `infrastructure/staging.env.example`
- Production env template: `infrastructure/production.env.example`
- Rollback script: `scripts/rollback.sh`

## Configuration Options
| Area | Setting | Description |
|---|---|---|
| ASP.NET Core | `ASPNETCORE_ENVIRONMENT` | Environment selection (`Development`, `Testing`, `Staging`, `Production`) |
| Database | `ConnectionStrings__Default` | PostgreSQL connection string; fallback to in-memory when missing |
| Redis | `Redis__Connection` | Cache/session backend connection |
| Keycloak | `Keycloak__ServerUrl` | External IdP endpoint for production auth integration |
| Vault | `Vault__Enabled` / `Vault__Address` / `Vault__Token` / `Vault__Path` | Optional HashiCorp Vault secret bootstrap at API startup |
| Metrics | `PROMETHEUS_*` | Prometheus scrape and alert configuration via infrastructure files |
| Deployment | `DOCKER_IMAGE_TAG` | Image tag used by deploy workflow scripts |

## Troubleshooting Guide
- API starts but endpoints return empty data:
  Ensure persistence is configured; without `ConnectionStrings__Default`, the API uses an in-memory database.
- Swagger unavailable:
  Verify environment is `Development` or `Testing`, then open `/swagger`.
- E2E tests time out:
  Confirm `dotnet run --project src/API --urls http://127.0.0.1:5188` is reachable.
- Coverage gate failing in CI:
  Run unit tests locally with coverage parameters and inspect uncovered service paths.
- High latency in route/combat operations:
  Check Grafana dashboards and `RouteCalculationDuration` / `CombatTickDuration` metrics.
- Messaging websocket disconnects:
  Validate `playerId` query string and channel type/key formatting.
