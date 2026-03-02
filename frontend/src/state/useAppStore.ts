import { create } from "zustand";
import { loadDashboard, loadMarketSeries } from "../api/client";
import type { DashboardSnapshot, MarketPoint, RealtimeEvent, ScreenKey } from "../types";

type AppState = {
  activeScreen: ScreenKey;
  dashboard: DashboardSnapshot | null;
  marketSeries: MarketPoint[];
  recentEvents: RealtimeEvent[];
  online: boolean;
  pendingOutbound: number;
  setScreen: (screen: ScreenKey) => void;
  setOnline: (online: boolean) => void;
  setPendingOutbound: (count: number) => void;
  bootstrap: () => Promise<void>;
  applyEventBatch: (batch: RealtimeEvent[]) => void;
};

export const useAppStore = create<AppState>((set, get) => ({
  activeScreen: "dashboard",
  dashboard: null,
  marketSeries: [],
  recentEvents: [],
  online: true,
  pendingOutbound: 0,
  setScreen: (screen) => set({ activeScreen: screen }),
  setOnline: (online) => set({ online }),
  setPendingOutbound: (count) => set({ pendingOutbound: count }),
  bootstrap: async () => {
    const [dashboard, marketSeries] = await Promise.all([loadDashboard(), loadMarketSeries()]);
    set({ dashboard, marketSeries });
  },
  applyEventBatch: (batch) => {
    const prior = get();
    const nextDashboard = prior.dashboard ? { ...prior.dashboard } : null;
    let nextMarket = prior.marketSeries.slice();

    for (const event of batch) {
      if (event.type === "market.tick") {
        const point = {
          t: event.payload.updatedAt,
          price: 120 + event.payload.volatility * 30,
          demand: 0.8 + event.payload.volatility * 0.2
        };
        nextMarket = [...nextMarket.slice(-24), point];
      }

      if (event.type === "fleet.status" && nextDashboard) {
        nextDashboard.fleetStrength = Math.round(event.payload.convoyScore * 10);
      }

      if (event.type === "reputation.update" && nextDashboard) {
        nextDashboard.reputation = event.payload.alignment;
      }

      if (event.type === "connection.state") {
        set({ online: event.payload.online });
      }
    }

    set({
      dashboard: nextDashboard,
      marketSeries: nextMarket,
      recentEvents: [...batch, ...prior.recentEvents].slice(0, 80)
    });
  }
}));
