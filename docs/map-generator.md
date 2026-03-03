# Map Generator Guide

The Map Generator is a separate WPF desktop app used to generate, inspect, and publish navigation maps.

## Launch

```bash
dotnet run --project src/MapGenerator
```

## Core Workflows

- `Generate Preview`: creates a deterministic local sector/route layout from seed, sector count, and route density.
- `Load Current Map`: fetches sectors/routes from the API database and renders current snapshot rows.
- `Publish To DB`: creates sectors/routes in API storage; optionally replaces existing map first.

## Authentication

The app supports bearer token authentication in `Bearer Token`.

- Use `Sign In...` to obtain an access token.
- The login flow first tries `POST /api/auth/login` on the configured API base URL.
- If API login is unavailable, it falls back to Keycloak token login (realm `galactictrader` by default).
- The token is applied as `Authorization: Bearer <token>` on load and publish requests.
- `GT_KEYCLOAK_BASE_URL`, `GT_KEYCLOAK_REALM`, and `GT_KEYCLOAK_CLIENT_ID` can override Keycloak login defaults.
- `GT_LOG_SERVER_URL` and optional `GT_LOG_SERVER_API_KEY` forward Map Generator logs to a central Seq-compatible server.

## Inputs

- `API Base URL`: target API host (for example `http://localhost:8080`).
- `Seed`: deterministic map seed.
- `Sector Count`: number of sectors to generate.
- `Route Density`: route fan-out density (recommended `1` to `4`).
- `Replace existing map data`: deletes existing routes and sectors before publish.

## Operational Notes

- Use `Load Current Map` before replacing data to validate existing topology.
- Publish creates sectors first, then routes, so route references remain valid.
- When replacing map data, routes are deleted before sectors to preserve foreign key order.
