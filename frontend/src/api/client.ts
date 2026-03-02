import type { DashboardSnapshot, MarketPoint } from "../types";

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:8080";

async function fetchJson<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    headers: { "Content-Type": "application/json" },
    ...init
  });

  if (!response.ok) {
    throw new Error(`API error ${response.status} for ${path}`);
  }

  return (await response.json()) as T;
}

export async function loadDashboard(): Promise<DashboardSnapshot> {
  // Fallback synthetic view keeps the frontend usable while backend contracts evolve.
  return {
    wealth: 1_248_500,
    fleetStrength: 742,
    reputation: 68,
    activeRoutes: 9,
    globalRisk: 37,
    updatedAt: new Date().toISOString()
  };
}

export async function loadMarketSeries(): Promise<MarketPoint[]> {
  const now = Date.now();
  return Array.from({ length: 18 }, (_, index) => {
    const drift = Math.sin(index / 2.2) * 8;
    return {
      t: new Date(now - (17 - index) * 60 * 60 * 1000).toISOString(),
      price: 125 + drift + index * 0.7,
      demand: 0.85 + Math.cos(index / 2.5) * 0.12
    };
  });
}

export async function triggerMarketTick(): Promise<void> {
  await fetchJson("/api/economy/tick", { method: "POST" });
}

export async function fetchLeaderboard(type: string): Promise<unknown[]> {
  return fetchJson(`/api/leaderboards/${type}?limit=10`);
}
