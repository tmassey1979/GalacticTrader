export type ScreenKey =
  | "dashboard"
  | "trade"
  | "routes"
  | "fleet"
  | "battle"
  | "reputation"
  | "intelligence";

export type RealtimeEvent =
  | { type: "market.tick"; payload: { volatility: number; topCommodity: string; updatedAt: string } }
  | { type: "fleet.status"; payload: { activeShips: number; convoyScore: number } }
  | { type: "combat.result"; payload: { victoryRate: number; avgDamage: number } }
  | { type: "reputation.update"; payload: { alignment: number; factionTier: string } }
  | { type: "connection.state"; payload: { online: boolean } };

export type DashboardSnapshot = {
  wealth: number;
  fleetStrength: number;
  reputation: number;
  activeRoutes: number;
  globalRisk: number;
  updatedAt: string;
};

export type MarketPoint = {
  t: string;
  price: number;
  demand: number;
};
