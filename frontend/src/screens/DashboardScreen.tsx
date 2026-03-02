import { KpiCard } from "../components/KpiCard";
import { LineChart } from "../components/LineChart";
import type { DashboardSnapshot, MarketPoint } from "../types";

type Props = {
  snapshot: DashboardSnapshot | null;
  marketSeries: MarketPoint[];
  online: boolean;
};

export function DashboardScreen({ snapshot, marketSeries, online }: Props) {
  if (!snapshot) {
    return <section className="panel">Loading command telemetry...</section>;
  }

  return (
    <section className="screen-grid">
      <div className="kpi-grid">
        <KpiCard label="Wealth Overview" value={`$${snapshot.wealth.toLocaleString()}`} accent="amber" hint="Liquid + strategic holdings" />
        <KpiCard label="Fleet Strength" value={snapshot.fleetStrength.toString()} accent="teal" hint="Escort + convoy confidence" />
        <KpiCard label="Reputation" value={snapshot.reputation.toString()} hint="Lawful/dirty trajectory" />
        <KpiCard label="Active Routes" value={snapshot.activeRoutes.toString()} hint="Autopilot sessions + convoy lanes" />
      </div>

      <article className="panel wide">
        <header className="panel-header">
          <h2>Global Metrics Feed</h2>
          <span className={`status-dot ${online ? "online" : "offline"}`}>{online ? "Realtime online" : "Offline mode"}</span>
        </header>
        <LineChart points={marketSeries.map((point) => ({ x: point.t.slice(11, 16), y: point.price }))} />
      </article>

      <article className="panel">
        <h3>Risk Pulse</h3>
        <p className="hero-metric">{snapshot.globalRisk}%</p>
        <p>Pirate activity and route volatility blended index.</p>
      </article>
    </section>
  );
}
