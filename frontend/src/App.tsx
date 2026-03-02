import { useEffect, useMemo } from "react";
import { Sidebar } from "./components/Sidebar";
import { RealtimeSocketClient } from "./realtime/wsClient";
import { BattleResultsScreen } from "./screens/BattleResultsScreen";
import { DashboardScreen } from "./screens/DashboardScreen";
import { FleetManagementScreen } from "./screens/FleetManagementScreen";
import { MarketIntelligenceScreen } from "./screens/MarketIntelligenceScreen";
import { ReputationServicesScreen } from "./screens/ReputationServicesScreen";
import { RoutePlanningScreen } from "./screens/RoutePlanningScreen";
import { TradeScreen } from "./screens/TradeScreen";
import { useAppStore } from "./state/useAppStore";
import type { RealtimeEvent } from "./types";

function getOrCreatePlayerId(): string {
  const existing = localStorage.getItem("gt-player-id");
  if (existing) {
    return existing;
  }
  const next = crypto.randomUUID();
  localStorage.setItem("gt-player-id", next);
  return next;
}

export default function App() {
  const activeScreen = useAppStore((state) => state.activeScreen);
  const setScreen = useAppStore((state) => state.setScreen);
  const dashboard = useAppStore((state) => state.dashboard);
  const marketSeries = useAppStore((state) => state.marketSeries);
  const online = useAppStore((state) => state.online);
  const recentEvents = useAppStore((state) => state.recentEvents);
  const bootstrap = useAppStore((state) => state.bootstrap);
  const applyEventBatch = useAppStore((state) => state.applyEventBatch);
  const setOnline = useAppStore((state) => state.setOnline);

  const wsUrl = useMemo(() => {
    const base = import.meta.env.VITE_WS_BASE_URL ?? "ws://localhost:8080";
    return `${base}/api/communication/ws/global/global?playerId=${getOrCreatePlayerId()}`;
  }, []);

  useEffect(() => {
    void bootstrap();
  }, [bootstrap]);

  useEffect(() => {
    const realtime = new RealtimeSocketClient(wsUrl, (batch) => applyEventBatch(batch));
    realtime.onStatus((isOnline) => {
      applyEventBatch([{ type: "connection.state", payload: { online: isOnline } }]);
      setOnline(isOnline);
    });
    realtime.start();

    // Local heartbeat keeps dashboards fresh when backend stream is quiet.
    const pulse = window.setInterval(() => {
      const syntheticEvent: RealtimeEvent = {
        type: "market.tick",
        payload: {
          volatility: Math.random(),
          topCommodity: "SynthFiber",
          updatedAt: new Date().toISOString()
        }
      };
      applyEventBatch([syntheticEvent]);
    }, 6_000);

    return () => {
      clearInterval(pulse);
      realtime.stop();
    };
  }, [applyEventBatch, setOnline, wsUrl]);

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

        {activeScreen === "dashboard" && <DashboardScreen snapshot={dashboard} marketSeries={marketSeries} online={online} />}
        {activeScreen === "trade" && <TradeScreen marketSeries={marketSeries} />}
        {activeScreen === "routes" && <RoutePlanningScreen />}
        {activeScreen === "fleet" && <FleetManagementScreen />}
        {activeScreen === "battle" && <BattleResultsScreen />}
        {activeScreen === "reputation" && <ReputationServicesScreen />}
        {activeScreen === "intelligence" && <MarketIntelligenceScreen />}
      </main>
    </div>
  );
}
