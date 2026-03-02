import { describe, expect, it } from "vitest";
import { defaultPreferences } from "../preferences/defaultPreferences";
import type { AppStateData } from "./storeTypes";
import { reduceRealtimeBatch } from "./realtimeBatchReducer";

function createBaseState(): AppStateData {
  return {
    activeScreen: "dashboard",
    dashboard: {
      wealth: 1000,
      fleetStrength: 50,
      reputation: 12,
      activeRoutes: 3,
      globalRisk: 20,
      updatedAt: "2026-03-02T00:00:00.000Z"
    },
    marketSeries: [],
    recentEvents: [],
    online: true,
    pendingOutbound: 0,
    preferences: defaultPreferences
  };
}

describe("reduceRealtimeBatch", () => {
  it("projects dashboard, market and connection updates from event batches", () => {
    const prior = createBaseState();
    const next = reduceRealtimeBatch(prior, [
      {
        type: "market.tick",
        payload: {
          volatility: 0.5,
          topCommodity: "SynthFiber",
          updatedAt: "2026-03-02T12:34:56.000Z"
        }
      },
      {
        type: "fleet.status",
        payload: {
          activeShips: 4,
          convoyScore: 8.2
        }
      },
      {
        type: "reputation.update",
        payload: {
          alignment: 31,
          factionTier: "trusted"
        }
      },
      {
        type: "connection.state",
        payload: {
          online: false
        }
      }
    ]);

    expect(next.online).toBe(false);
    expect(next.dashboard?.fleetStrength).toBe(82);
    expect(next.dashboard?.reputation).toBe(31);
    expect(next.marketSeries).toHaveLength(1);
    expect(next.marketSeries[0]).toEqual({
      t: "2026-03-02T12:34:56.000Z",
      price: 135,
      demand: 0.9
    });
    expect(next.recentEvents).toHaveLength(4);
  });

  it("retains only the latest 80 events in the buffer", () => {
    const prior = createBaseState();
    prior.recentEvents = Array.from({ length: 80 }, (_, index) => ({
      type: "combat.result",
      payload: {
        victoryRate: 0.5,
        avgDamage: index
      }
    }));

    const next = reduceRealtimeBatch(prior, [
      {
        type: "connection.state",
        payload: { online: true }
      }
    ]);

    expect(next.recentEvents).toHaveLength(80);
    expect(next.recentEvents[0].type).toBe("connection.state");
  });
});
