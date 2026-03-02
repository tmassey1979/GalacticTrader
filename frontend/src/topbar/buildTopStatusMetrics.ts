import type { DashboardSnapshot } from "../types";
import type { TopStatusMetric } from "./TopStatusMetric";

type TopStatusContext = {
  dashboard: DashboardSnapshot | null;
  online: boolean;
  bufferedEvents: number;
};

const fallbackDashboard: DashboardSnapshot = {
  wealth: 0,
  fleetStrength: 0,
  reputation: 0,
  activeRoutes: 0,
  globalRisk: 0,
  updatedAt: new Date(0).toISOString()
};

export function buildTopStatusMetrics(context: TopStatusContext): TopStatusMetric[] {
  const snapshot = context.dashboard ?? fallbackDashboard;

  const playerName = "Commander Orin Vale";
  const protectionStatus = snapshot.globalRisk <= 40 ? "Guarded" : snapshot.globalRisk <= 65 ? "Contested" : "Vulnerable";
  const liquidCredits = Math.max(0, Math.round(snapshot.wealth * 0.36));
  const alerts = context.online ? (snapshot.globalRisk > 60 ? 2 : 0) : 1;
  const globalEconomicIndex = Math.max(0, Math.min(100, Math.round(100 - snapshot.globalRisk + (snapshot.reputation * 0.2))));

  return [
    makeMetric("player", "Player", playerName, "Current command profile identity.", [73, 76, 78, 79, 82]),
    makeMetric("reputation", "Reputation", `${snapshot.reputation}`, "Faction trust and policy influence score.", trendFrom(snapshot.reputation, 3)),
    makeMetric("net-worth", "Net Worth", formatCurrency(snapshot.wealth), "Total strategic asset valuation across systems.", trendFrom(snapshot.wealth / 100_000, 4)),
    makeMetric("liquid-credits", "Liquid Credits", formatCurrency(liquidCredits), "Immediately available credits for orders and contracts.", trendFrom(liquidCredits / 100_000, 2)),
    makeMetric("fleet-strength", "Fleet Strength", `${snapshot.fleetStrength}`, "Current operational convoy and escort capacity.", trendFrom(snapshot.fleetStrength, 3)),
    makeMetric("protection-status", "Protection", protectionStatus, "Insurance and patrol posture over active territory.", trendFrom(snapshot.globalRisk * -1, 2)),
    makeMetric("active-routes", "Active Routes", `${snapshot.activeRoutes}`, "Routes currently running under command authority.", trendFrom(snapshot.activeRoutes * 6, 2)),
    makeMetric("alerts", "Alerts", `${alerts}`, "Priority tactical and economic risk notifications.", trendFrom(alerts * 16, 1)),
    makeMetric("global-economic-index", "Global Econ Index", `${globalEconomicIndex}`, "Macro market stability index across sectors.", trendFrom(globalEconomicIndex, 3))
  ];
}

function makeMetric(id: string, label: string, value: string, tooltip: string, trend: number[]): TopStatusMetric {
  return { id, label, value, tooltip, trend };
}

function formatCurrency(value: number): string {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD", maximumFractionDigits: 0 }).format(value);
}

function trendFrom(seed: number, variance: number): number[] {
  const baseline = Math.max(12, Math.min(88, Math.round(seed % 100)));
  return [0, 1, 2, 3, 4].map((step) => Math.max(8, Math.min(92, baseline - variance + step * variance)));
}
