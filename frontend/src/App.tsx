import { useEffect, useMemo } from "react";
import { EventFeed } from "./components/EventFeed";
import { Sidebar } from "./components/Sidebar";
import { TopStatusBar } from "./components/TopStatusBar";
import { buildAnalyticsCsv } from "./export/buildAnalyticsCsv";
import { buildEventsCsv } from "./export/buildEventsCsv";
import { downloadCsv } from "./export/downloadCsv";
import type { AppPreferences } from "./preferences/AppPreferences";
import { createHeartbeatMarketTick } from "./realtime/heartbeatEventFactory";
import { buildGlobalRealtimeUrl } from "./realtime/globalRealtimeUrl";
import { RealtimeSocketClient } from "./realtime/wsClient";
import { resolveScreenHotkey } from "./shortcuts/resolveScreenHotkey";
import { AnalyticsScreen } from "./screens/AnalyticsScreen";
import { BattleResultsScreen } from "./screens/BattleResultsScreen";
import { DashboardScreen } from "./screens/DashboardScreen";
import { FleetManagementScreen } from "./screens/FleetManagementScreen";
import { MarketIntelligenceScreen } from "./screens/MarketIntelligenceScreen";
import { ReputationServicesScreen } from "./screens/ReputationServicesScreen";
import { RoutePlanningScreen } from "./screens/RoutePlanningScreen";
import { SettingsScreen } from "./screens/SettingsScreen";
import { TerritoryScreen } from "./screens/TerritoryScreen";
import { TradeScreen } from "./screens/TradeScreen";
import { getOrCreatePlayerId } from "./session/playerSession";
import { useAppStore } from "./state/useAppStore";
import { buildTopStatusMetrics } from "./topbar/buildTopStatusMetrics";

export default function App() {
  const activeScreen = useAppStore((state) => state.activeScreen);
  const setScreen = useAppStore((state) => state.setScreen);
  const dashboard = useAppStore((state) => state.dashboard);
  const marketSeries = useAppStore((state) => state.marketSeries);
  const online = useAppStore((state) => state.online);
  const recentEvents = useAppStore((state) => state.recentEvents);
  const preferences = useAppStore((state) => state.preferences);
  const updatePreferences = useAppStore((state) => state.updatePreferences);
  const bootstrap = useAppStore((state) => state.bootstrap);
  const applyEventBatch = useAppStore((state) => state.applyEventBatch);
  const statusMetrics = useMemo(
    () =>
      buildTopStatusMetrics({
        dashboard,
        online,
        bufferedEvents: recentEvents.length
      }),
    [dashboard, online, recentEvents.length]
  );

  const wsUrl = useMemo(() => {
    const base = import.meta.env.VITE_WS_BASE_URL ?? "ws://localhost:8080";
    return buildGlobalRealtimeUrl(base, getOrCreatePlayerId());
  }, []);

  useEffect(() => {
    void bootstrap();
  }, [bootstrap]);

  useEffect(() => {
    const realtime = new RealtimeSocketClient(wsUrl, (batch) => applyEventBatch(batch));
    realtime.onStatus((isOnline) => {
      applyEventBatch([{ type: "connection.state", payload: { online: isOnline } }]);
    });
    realtime.start();

    return () => {
      realtime.stop();
    };
  }, [applyEventBatch, wsUrl]);

  useEffect(() => {
    // Local heartbeat keeps dashboards fresh when backend stream is quiet.
    const pulse = window.setInterval(() => {
      applyEventBatch([createHeartbeatMarketTick()]);
    }, preferences.heartbeatIntervalMs);

    return () => {
      clearInterval(pulse);
    };
  }, [applyEventBatch, preferences.heartbeatIntervalMs]);

  useEffect(() => {
    if (!preferences.keyboardShortcutsEnabled) {
      return;
    }

    const handler = (event: KeyboardEvent) => {
      const target = event.target as HTMLElement | null;
      if (target && (target.tagName === "INPUT" || target.tagName === "SELECT" || target.tagName === "TEXTAREA")) {
        return;
      }

      const nextScreen = resolveScreenHotkey(event.key);
      if (!nextScreen) {
        return;
      }

      setScreen(nextScreen);
    };

    window.addEventListener("keydown", handler);
    return () => window.removeEventListener("keydown", handler);
  }, [preferences.keyboardShortcutsEnabled, setScreen]);

  function handlePreferencesChange(next: Partial<AppPreferences>) {
    updatePreferences(next);
  }

  function handleExportSelectedCsv() {
    if (preferences.csvExportTarget === "analytics") {
      downloadCsv("analytics-metrics.csv", buildAnalyticsCsv(statusMetrics));
      return;
    }

    downloadCsv("event-feed.csv", buildEventsCsv(recentEvents));
  }

  return (
    <div className="app-shell">
      <Sidebar active={activeScreen} onChange={setScreen} />
      <main className={`content ${preferences.compactPanels ? "compact-density" : ""}`}>
        <header className="topbar">
          <div>
            <p className="eyebrow">Galactic Trader</p>
            <h1>Strategic Operations Deck</h1>
          </div>
          <div className="topbar-right">
            <span className={`status-dot ${online ? "online" : "offline"}`}>{online ? "Realtime linked" : "Offline queueing"}</span>
            <span className="event-count">{recentEvents.length} buffered events</span>
          </div>
        </header>
        <TopStatusBar metrics={statusMetrics} />

        {activeScreen === "dashboard" && <DashboardScreen snapshot={dashboard} marketSeries={marketSeries} online={online} />}
        {activeScreen === "trade" && <TradeScreen marketSeries={marketSeries} />}
        {activeScreen === "routes" && <RoutePlanningScreen />}
        {activeScreen === "fleet" && <FleetManagementScreen />}
        {activeScreen === "battle" && <BattleResultsScreen />}
        {activeScreen === "reputation" && <ReputationServicesScreen />}
        {activeScreen === "territory" && <TerritoryScreen />}
        {activeScreen === "analytics" && <AnalyticsScreen />}
        {activeScreen === "intelligence" && <MarketIntelligenceScreen />}
        {activeScreen === "settings" && (
          <SettingsScreen preferences={preferences} onChange={handlePreferencesChange} onExportSelectedCsv={handleExportSelectedCsv} />
        )}
        <EventFeed events={recentEvents} />
      </main>
    </div>
  );
}
