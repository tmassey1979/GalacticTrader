# API Swagger/OpenAPI Guide

## Status
Complete

## Swagger/OpenAPI setup
- Swagger UI: `http://localhost:8080/swagger`
- OpenAPI JSON: `http://localhost:8080/swagger/v1/swagger.json`
- OpenAPI endpoint (development/testing): `http://localhost:8080/openapi/v1.json`
- Security scheme: HTTP Bearer (`Authorization: Bearer {token}`)

The API now includes:
- Global Swagger metadata (`Galactic Trader API`, `v1`)
- Bearer auth security definition
- Default error response documentation for all operations
- Request schema examples for core request payloads

## Authentication docs

### Register
```bash
curl -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "captain_hera",
    "email": "hera@galactictrader.test",
    "password": "WarpDrive9000",
    "firstName": "Hera",
    "lastName": "Syndulla",
    "birthdate": "1990-04-18",
    "gender": "Woman",
    "pronouns": "she/her",
    "locale": "en-US",
    "timeZone": "America/Chicago",
    "phoneNumber": "+1-555-0100"
  }'
```

### Login
```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "captain_hera",
    "password": "WarpDrive9000"
  }'
```

### Validate token
```bash
curl "http://localhost:8080/api/auth/validate?token={accessToken}"
```

## Communication Voice Flow Examples

Create a voice channel:
```bash
curl -X POST http://localhost:8080/api/communication/voice/channels \
  -H "Content-Type: application/json" \
  -d '{
    "creatorPlayerId": "11111111-1111-1111-1111-111111111111",
    "mode": 0,
    "scopeKey": "desktop-feed"
  }'
```

Join voice channel:
```bash
curl -X POST http://localhost:8080/api/communication/voice/channels/{channelId}/join \
  -H "Content-Type: application/json" \
  -d '{
    "playerId": "22222222-2222-2222-2222-222222222222"
  }'
```

Report voice activity:
```bash
curl -X POST http://localhost:8080/api/communication/voice/channels/{channelId}/activity \
  -H "Content-Type: application/json" \
  -d '{
    "playerId": "11111111-1111-1111-1111-111111111111",
    "rmsLevel": 0.6,
    "packetLossPercent": 0.3,
    "latencyMs": 32,
    "jitterMs": 3.8
  }'
```

Publish and poll voice signal:
```bash
curl -X POST http://localhost:8080/api/communication/voice/channels/{channelId}/signal \
  -H "Content-Type: application/json" \
  -d '{
    "senderId": "11111111-1111-1111-1111-111111111111",
    "targetPlayerId": "22222222-2222-2222-2222-222222222222",
    "signalType": "offer",
    "payload": "sdp-offer"
  }'

curl "http://localhost:8080/api/communication/voice/channels/{channelId}/signals/22222222-2222-2222-2222-222222222222?limit=25"
```

Preview spatial audio mix:
```bash
curl -X POST http://localhost:8080/api/communication/voice/channels/{channelId}/spatial-audio \
  -H "Content-Type: application/json" \
  -d '{
    "listenerId": "22222222-2222-2222-2222-222222222222",
    "listenerX": 0,
    "listenerY": 0,
    "listenerZ": 0,
    "falloffDistance": 100,
    "speakers": [
      {
        "playerId": "11111111-1111-1111-1111-111111111111",
        "x": 18,
        "y": 0,
        "z": 0,
        "baseGain": 1
      }
    ]
  }'
```

## Endpoint documentation coverage
All endpoint groups are documented in Swagger and emitted into `swagger/v1/swagger.json`:
- Authentication
- Navigation (sectors, routes, planning, autopilot, graph)
- Combat
- Economy
- Market
- NPC
- Fleet
- Reputation
- Leaderboards
- Communication (text, websocket, voice)
- Telemetry (`/metrics`)

## Request/response examples
Examples are included in OpenAPI schema metadata for:
- `RegisterPlayerApiRequest`
- `LoginPlayerApiRequest`
- `CreateSectorRequest`
- `CreateRouteRequest`

Representative response patterns:
- `201 Created` with resource payload for create operations
- `200 OK` with DTO payload for read/update flows
- `202 Accepted` for async-accepted actions
- `204 No Content` for successful delete/leave/cancel operations

## Error code reference
- `400 Bad Request`: invalid payload, validation failure, or business rule rejection
- `401 Unauthorized`: invalid login/token
- `404 Not Found`: missing resource identifiers
- `409 Conflict`: duplicate registration / uniqueness conflicts
- `500 Internal Server Error`: unhandled server-side faults

## Client SDK generation
Use the generated script to produce SDKs from Swagger JSON:
- PowerShell: `scripts/generate-sdk.ps1`
- Bash: `scripts/generate-sdk.sh`

Example:
```powershell
./scripts/generate-sdk.ps1 -ApiBaseUrl "http://localhost:8080" -OutputDir "generated-clients"
```

This generates:
- TypeScript client (`generated-clients/typescript`)
- C# client (`generated-clients/csharp`)
