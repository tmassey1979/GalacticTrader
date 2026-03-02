# GALACTIC TRADER

## Architecture & Infrastructure Codex

### Version 1.0

------------------------------------------------------------------------

# I. Architectural Philosophy

Galactic Trader is built as a server-authoritative, horizontally
scalable, microservice-driven simulation platform.

Design Goals:

-   Deterministic backend simulation
-   Real-time economic computation
-   Horizontal scalability
-   Observability-first architecture
-   Container-native deployment
-   Zero client authority for gameplay logic

------------------------------------------------------------------------

# II. High-Level System Architecture

Clients: - WPF .NET 9 Desktop Client - 3D Rendering Layer (embedded
engine) - Web Admin Console

Backend Core: - .NET 10 REST APIs - Microservices Architecture -
PostgreSQL (Primary DB) - Redis (Caching + Fast State) - Keycloak
(Identity Provider) - Prometheus (Metrics) - Grafana (Dashboards)

Infrastructure: - Docker Containers - Docker Compose (Dev) - Reverse
Proxy (YARP or NGINX) - Optional Kubernetes (Future Scale)

------------------------------------------------------------------------

# III. Service Architecture

## API Gateway

Responsibilities: - Route traffic - JWT validation - Rate limiting - API
aggregation

## Identity Service

External via Keycloak: - OAuth2 / OIDC - JWT issuance - Role-based
access control

## Navigation Service

-   Route calculations (A\* / Dijkstra)
-   Sector graph caching
-   Autopilot state machine

## Combat Service

-   Tick-based deterministic engine
-   Subsystem resolution
-   Combat logs persistence

## Economy Service

-   Dynamic pricing engine
-   Market recalculation
-   Trade volume tracking

## Market Service

-   Transaction validation
-   Inventory updates
-   Supply/Demand recalculation

## NPC Service

-   Archetype behavior engine
-   Decision weighting logic
-   Autonomous route planning
-   Combat engagement decision

## Fleet Service

-   Fleet composition management
-   Escort calculations
-   Convoy bonus logic

## Communication Service

-   Text messaging storage
-   WebRTC session coordination
-   Channel management

## Telemetry Service

-   Prometheus instrumentation
-   Metric aggregation
-   Leaderboard computation

------------------------------------------------------------------------

# IV. Data Architecture

Primary Database: PostgreSQL

Core Tables: - Players - Ships - Crew - Sectors - Routes - Factions -
Markets - TradeTransactions - CombatLogs - NPCState - Leaderboards

Indexes: - PlayerId - SectorId - FactionId - MarketId - Timestamp

Caching Layer: Redis

Used For: - Active session tracking - Route cache - Leaderboard
snapshots - Combat tick state - Sector heatmaps

------------------------------------------------------------------------

# V. Simulation Engine Design

## Tick Engine

-   Resolution: 250ms (combat)
-   Resolution: 1s (navigation + NPC decisions)
-   Stateless REST endpoints
-   Stateful simulation workers

Each tick: 1. Update ship states 2. Evaluate encounters 3. Process
combat ticks 4. Update economic deltas 5. Emit telemetry events

------------------------------------------------------------------------

# VI. Infrastructure Deployment

## Dockerized Services

Containers: - api-gateway - navigation-service - combat-service -
economy-service - npc-service - fleet-service - communication-service -
postgres - redis - keycloak - prometheus - grafana

Network: - Internal Docker network - Gateway exposed externally

------------------------------------------------------------------------

# VII. Observability & Monitoring

Prometheus Metrics:

-   api_request_duration_seconds
-   combat_tick_duration_seconds
-   route_calculation_time_seconds
-   db_query_duration_seconds
-   redis_cache_hit_ratio
-   active_users_current
-   active_battles_current
-   total_currency_in_circulation

Grafana Dashboards:

1.  Live System Health
2.  Economic Stability
3.  Sector Risk Heatmap
4.  NPC vs Player Influence
5.  Combat Distribution

------------------------------------------------------------------------

# VIII. Security Architecture

-   JWT validation middleware
-   Role-based endpoint protection
-   Service-to-service authentication
-   Database role separation
-   Rate limiting on gateway
-   Anti-exploit anomaly detection

------------------------------------------------------------------------

# IX. Scaling Strategy

Phase 1: - Single-node Docker deployment

Phase 2: - Separate DB and Redis host - Horizontal scaling of stateless
services

Phase 3: - Kubernetes cluster - Dedicated simulation workers -
Load-balanced gateway

------------------------------------------------------------------------

# X. Backup & Resilience

-   Nightly PostgreSQL backups
-   Redis snapshotting
-   Health checks on all services
-   Automatic container restart policies
-   Graceful shutdown of tick engines

------------------------------------------------------------------------

# XI. CI/CD Strategy

-   Git-based version control
-   Automated build pipeline
-   Docker image publishing
-   Versioned deployments
-   Environment-based config (Dev / Staging / Prod)

------------------------------------------------------------------------

# XII. Performance Targets

-   Combat tick under 50ms processing
-   Route calculation under 20ms average
-   API p95 latency under 150ms
-   1,000+ concurrent users scalable
-   Deterministic simulation under load

------------------------------------------------------------------------

# XIII. Future Infrastructure Enhancements

-   Event Bus (Kafka or RabbitMQ)
-   Distributed caching layer
-   AI model service for advanced NPC behavior
-   Edge CDN for asset distribution
-   Cross-region deployment

------------------------------------------------------------------------

End of Architecture & Infrastructure Codex
