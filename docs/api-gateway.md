# API Gateway

GalacticTrader now runs a dedicated YARP gateway (`src/Gateway`) in front of the API service.

## Responsibilities
- Route traffic into backend services.
- Enforce JWT authentication for protected API paths.
- Apply global fixed-window rate limiting.
- Expose lightweight gateway health endpoints.

## Runtime Endpoints
- Gateway base URL (Docker compose): `http://localhost:8081`
- Liveness: `GET /health/live`
- Readiness: `GET /health/ready`

## Route Policies
- Open routes (no gateway JWT policy):
  - `/api/auth/**`
  - `/swagger/**`
  - `/openapi/**`
  - `/metrics`
- Protected routes (JWT required):
  - `/api/**`

## Configuration
Environment variables (or config keys):
- `Gateway__ApiBaseUrl` (default `http://api:8080`)
- `Gateway__Jwt__Authority` (default `http://keycloak:8080/realms/galactictrader`)
- `Gateway__Jwt__Audience` (default `account`)
- `Gateway__Jwt__RequireHttpsMetadata` (default `false` for local dev)
- `Gateway__RateLimit__PermitLimit` (default `300`)
- `Gateway__RateLimit__WindowSeconds` (default `60`)

## Smoke Check
Use one of:
- `./scripts/smoke-gateway.sh`
- `./scripts/smoke-gateway.ps1`

The smoke checks validate:
- Gateway health endpoint response.
- Open auth route behavior through the proxy.
- Protected API route JWT enforcement.
- Metrics route proxying.
