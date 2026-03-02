# GalacticTrader Frontend

## Setup
1. Install dependencies:
```bash
npm install
```
2. Copy environment file:
```bash
cp .env.example .env
```
3. Start the dev server:
```bash
npm run dev
```

## Architecture
- React + Vite + TypeScript project scaffold.
- Zustand store for client state and real-time merge logic.
- Centralized API client in `src/api/client.ts`.
- WebSocket client with reconnect, batching, outbound queueing, and offline handling in `src/realtime/wsClient.ts`.
- Screen modules:
  - Dashboard
  - Trade
  - Route Planning
  - Fleet Management
  - Battle Results
  - Reputation & Services
  - Market Intelligence
