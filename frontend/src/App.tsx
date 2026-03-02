import { useEffect, useMemo } from "react";
import { Sidebar } from "./components/Sidebar";
import { TopStatusBar } from "./components/TopStatusBar";
import { createHeartbeatMarketTick } from "./realtime/heartbeatEventFactory";
import { buildGlobalRealtimeUrl } from "./realtime/globalRealtimeUrl";
import { RealtimeSocketClient } from "./realtime/wsClient";
import { BattleResultsScreen } from "./screens/BattleResultsScreen";
import { DashboardScreen } from "./screens/DashboardScreen";
import { FleetManagementScreen } from "./screens/FleetManagementScreen";
import { MarketIntelligenceScreen } from "./screens/MarketIntelligenceScreen";
import { ReputationServicesScreen } from "./screens/ReputationServicesScreen";
import { RoutePlanningScreen } from "./screens/RoutePlanningScreen";
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

    // Local heartbeat keeps dashboards fresh when backend stream is quiet.
    const pulse = window.setInterval(() => {
      applyEventBatch([createHeartbeatMarketTick()]);
    }, 6_000);

    return () => {
      clearInterval(pulse);
      realtime.stop();
    };
  }, [applyEventBatch, wsUrl]);

  return (
    <div className="app-shell">
      <Sidebar active={activeScreen} onChange={setScreen} />
      <main className="content">
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
        {activeScreen === "intelligence" && <MarketIntelligenceScreen />}
      </main>
    </div>
  );
}
