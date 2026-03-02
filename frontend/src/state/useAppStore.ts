import { create } from "zustand";
import { loadDashboard, loadMarketSeries } from "../api/client";
import { loadAppPreferences, saveAppPreferences } from "../preferences/preferencesStorage";
import { reduceRealtimeBatch } from "./realtimeBatchReducer";
import { initialAppState, type AppState } from "./storeTypes";

export const useAppStore = create<AppState>((set, get) => ({
  ...initialAppState,
  preferences: loadAppPreferences(),
  setScreen: (screen) => set({ activeScreen: screen }),
  setOnline: (online) => set({ online }),
  setPendingOutbound: (count) => set({ pendingOutbound: count }),
  updatePreferences: (next) => {
    const merged = { ...get().preferences, ...next };
    saveAppPreferences(merged);
    set({ preferences: merged });
  },
  bootstrap: async () => {
    const [dashboard, marketSeries] = await Promise.all([loadDashboard(), loadMarketSeries()]);
    set({ dashboard, marketSeries });
  },
  applyEventBatch: (batch) => {
    const prior = get();
    set(reduceRealtimeBatch(prior, batch));
  }
}));
