import type { AppPreferences } from "../preferences/AppPreferences";
import { defaultPreferences } from "../preferences/defaultPreferences";
import type { DashboardSnapshot, MarketPoint, RealtimeEvent, ScreenKey } from "../types";

export type AppStateData = {
  activeScreen: ScreenKey;
  dashboard: DashboardSnapshot | null;
  marketSeries: MarketPoint[];
  recentEvents: RealtimeEvent[];
  online: boolean;
  pendingOutbound: number;
  preferences: AppPreferences;
};

export type AppStateActions = {
  setScreen: (screen: ScreenKey) => void;
  setOnline: (online: boolean) => void;
  setPendingOutbound: (count: number) => void;
  updatePreferences: (next: Partial<AppPreferences>) => void;
  bootstrap: () => Promise<void>;
  applyEventBatch: (batch: RealtimeEvent[]) => void;
};

export type AppState = AppStateData & AppStateActions;

export const initialAppState: AppStateData = {
  activeScreen: "dashboard",
  dashboard: null,
  marketSeries: [],
  recentEvents: [],
  online: true,
  pendingOutbound: 0,
  preferences: defaultPreferences
};
