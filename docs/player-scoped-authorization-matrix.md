# Player-Scoped Endpoint Authorization Matrix

This matrix defines policy for API endpoints that accept a player identifier in route or body.

| Endpoint | Scope | Policy | Notes |
|---|---|---|---|
| `GET /api/market/transactions/{playerId}` | REST read | Owner-or-admin | Financial history is private account data. |
| `GET /api/fleet/players/{playerId}/ships` | REST read | Owner-or-admin | Fleet inventory is private account data. |
| `GET /api/fleet/players/{playerId}/escort` | REST read | Owner-or-admin | Escort composition reveals tactical state. |
| `GET /api/reputation/factions/{playerId}` | REST read | Owner-or-admin | Faction standings are player-specific progression data. |
| `GET /api/reputation/factions/{playerId}/benefits` | REST read | Owner-or-admin | Benefits unlocks are private progression data. |
| `GET /api/reputation/alignment/{playerId}` | REST read | Owner-or-admin | Alignment state affects restricted gameplay access. |
| `GET /api/strategic/insurance/policies/{playerId}` | REST read | Owner-or-admin | Policy inventory is private strategic/account data. |
| `GET /api/strategic/insurance/claims/{playerId}` | REST read | Owner-or-admin | Claim history is private strategic/account data. |
| `GET /api/strategic/intelligence/reports/{playerId}` | REST read | Owner-or-admin | Intelligence reports are sensitive strategic data. |
| `GET /api/leaderboards/{leaderboardType}/player/{playerId}` | REST read | Public | Ranking position is intentionally public scoreboard data. |
| `GET /api/leaderboards/{leaderboardType}/player/{playerId}/history` | REST read | Public | Historical ranking trend is public scoreboard data. |
| `POST /api/communication/subscribe` (`playerId`) | REST mutation | Owner-or-admin | Caller identity is validated from bearer token. |
| `POST /api/communication/unsubscribe` (`playerId`) | REST mutation | Owner-or-admin | Caller identity is validated from bearer token. |
| `POST /api/communication/messages` (`playerId`) | REST mutation | Owner-or-admin | Sender identity is validated from bearer token. |
| `POST /api/communication/voice/channels` (`creatorPlayerId`) | REST mutation | Owner-or-admin | Channel creator identity is token-derived. |
| `POST /api/communication/voice/channels/{channelId}/join` (`playerId`) | REST mutation | Owner-or-admin | Join identity is token-derived. |
| `POST /api/communication/voice/channels/{channelId}/leave/{playerId}` | REST mutation | Owner-or-admin | Leave identity is token-derived. |
| `POST /api/communication/voice/channels/{channelId}/signal` (`senderId`) | REST mutation | Owner-or-admin | Sender identity is token-derived. |
| `GET /api/communication/voice/channels/{channelId}/signals/{playerId}` | REST read | Owner-or-admin | Signal queues are private per player. |
| `POST /api/communication/voice/channels/{channelId}/activity` (`playerId`) | REST mutation | Owner-or-admin | Activity updates are token-bound. |
| `POST /api/communication/voice/channels/{channelId}/spatial-audio` (`listenerId`) | REST mutation | Owner-or-admin | Listener context is token-bound. |
| `GET /api/strategic/ws/dashboard/{playerId}` | WebSocket | Owner-or-admin | Handshake is authorized before socket acceptance. |

## Behavior Rules

- Restricted endpoints return `401 Unauthorized` when bearer auth is missing or invalid.
- Restricted endpoints return `403 Forbidden` for authenticated callers who are not owner/admin.
- Public endpoints do not require auth and must not return `401/403` solely because auth is absent.
