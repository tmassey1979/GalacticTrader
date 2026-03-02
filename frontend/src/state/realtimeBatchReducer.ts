import type { RealtimeEvent } from "../types";
import type { AppStateData } from "./storeTypes";

type RealtimeProjection = Pick<AppStateData, "dashboard" | "marketSeries" | "recentEvents" | "online">;

export function reduceRealtimeBatch(prior: AppStateData, batch: RealtimeEvent[]): RealtimeProjection {
  const nextDashboard = prior.dashboard ? { ...prior.dashboard } : null;
  let nextMarket = prior.marketSeries.slice();
  let nextOnline = prior.online;

  for (const event of batch) {
    if (event.type === "market.tick") {
      const point = {
        t: event.payload.updatedAt,
        price: 120 + event.payload.volatility * 30,
        demand: 0.8 + event.payload.volatility * 0.2
      };
      nextMarket = [...nextMarket.slice(-24), point];
      continue;
    }

    if (event.type === "fleet.status" && nextDashboard) {
      nextDashboard.fleetStrength = Math.round(event.payload.convoyScore * 10);
      continue;
    }

    if (event.type === "reputation.update" && nextDashboard) {
      nextDashboard.reputation = event.payload.alignment;
      continue;
    }

    if (event.type === "connection.state") {
      nextOnline = event.payload.online;
    }
  }

  return {
    dashboard: nextDashboard,
    marketSeries: nextMarket,
    recentEvents: [...batch, ...prior.recentEvents].slice(0, 80),
    online: nextOnline
  };
}
